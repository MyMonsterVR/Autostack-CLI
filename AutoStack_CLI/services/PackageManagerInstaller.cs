using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoStack_CLI.models;

namespace AutoStack_CLI.services;

public static partial class PackageManagerInstaller
{
    public static async Task<bool> InstallPackages(List<Packages> packages, ConfigurationService configurationService)
    {
        var config = await configurationService.LoadConfigAsync();

        // Verify package manager is still available
        if (!await PackageManagerDetector.IsInstalledAsync(config.ChosenPackageManager))
        {
            Console.WriteLine($"Error: {config.ChosenPackageManager} is no longer available.");
            Console.WriteLine("Please restart the CLI and run setup to reconfigure your package manager.");
            return false;
        }

        var installCommand = GetInstallPmCommand(config);
        var packageNames = GetRealPackageNames(packages);

        if (packageNames.Length < 1) return false;

        var path = await SelectDestination(configurationService);

        // User cancelled destination selection
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("Installation cancelled.");
            return false;
        }

        var initDetails = new InitDetails
        {
            DetailsEnabled = CanInitializeWithDetails(config)
        };

        if(initDetails.DetailsEnabled)
        {
            // Get default project name from folder name
            var defaultProjectName = new DirectoryInfo(path).Name.ToLower().Replace(" ", "-");

            Console.WriteLine();
            Console.WriteLine("Please fill out the following details, press enter to use default:");
            Console.Write($"Project name? ({defaultProjectName}): ");
            initDetails.ProjectName = Console.ReadLine() ?? "";

            Console.Write("Version? (1.0.0): ");
            initDetails.Version = Console.ReadLine() ?? "";

            Console.Write("Description?: ");
            initDetails.Description = Console.ReadLine() ?? "";

            Console.Write("Entry point? (index.js): ");
            initDetails.EntryPoint = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(initDetails.ProjectName)) initDetails.ProjectName = defaultProjectName;
            if (string.IsNullOrWhiteSpace(initDetails.Version)) initDetails.Version = "1.0.0";
            if (string.IsNullOrWhiteSpace(initDetails.EntryPoint)) initDetails.EntryPoint = "index.js";

            Console.Write("Author(s)?: ");
            initDetails.Author = Console.ReadLine() ?? "";

            Console.WriteLine($"Project: {initDetails.ProjectName}");
            Console.WriteLine($"Version: {initDetails.Version}");
            Console.WriteLine($"Description: {initDetails.Description}");
            Console.WriteLine($"Entry point: {initDetails.EntryPoint}");
            Console.WriteLine($"Author(s): {initDetails.Author}");
            Console.WriteLine();
        }

        var pmName = GetPackageManagerName(config.ChosenPackageManager);
        const string initCommand = "init -y";
        var fullCommand = $"{pmName} {initCommand}";
        var (shellFileName, shellArguments) = GetShellCommand(path, fullCommand);

        var init = new ProcessStartInfo
        {
            FileName = shellFileName,
            Arguments = shellArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            Console.Clear();
            Console.WriteLine("Installing packages...");
            using var initProcess = Process.Start(init);
            if (initProcess == null)
            {
                Console.WriteLine($"Failed to start {config.ChosenPackageManager} process.");
                return false;
            }

            var output = await initProcess.StandardOutput.ReadToEndAsync();
            var error = await initProcess.StandardError.ReadToEndAsync();

            await initProcess.WaitForExitAsync();

            if (initProcess.ExitCode != 0)
            {
                Console.WriteLine($"{config.ChosenPackageManager} init failed with exit code {initProcess.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"Error output: {error}");
                }
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine($"Output: {output}");
                }
                return false;
            }

            // Update package.json with custom details if provided
            if (initDetails.DetailsEnabled)
            {
                await UpdatePackageJson(path, initDetails);
            }
         
            Console.WriteLine($"Packages has been installed to {path}");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            Console.WriteLine($"Error: {config.ChosenPackageManager} is not installed or not found in PATH.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  1. Install {config.ChosenPackageManager} and ensure it's in your PATH");
            Console.WriteLine("  2. Restart the CLI and run setup to choose a different package manager");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting {config.ChosenPackageManager}: {ex.Message}");
            return false;
        }
        
        var installFullCommand = $"{pmName} {installCommand} {packageNames}";
        var (installShellFileName, installShellArguments) = GetShellCommand(path, installFullCommand);

        var startInfo = new ProcessStartInfo
        {
            FileName = installShellFileName,
            Arguments = installShellArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var startInfoProcess = Process.Start(startInfo);
            if (startInfoProcess == null)
            {
                Console.WriteLine($"Failed to start {config.ChosenPackageManager} install process.");
                return false;
            }

            var installOutput = await startInfoProcess.StandardOutput.ReadToEndAsync();
            var installError = await startInfoProcess.StandardError.ReadToEndAsync();

            await startInfoProcess.WaitForExitAsync();

            if (startInfoProcess.ExitCode != 0)
            {
                Console.WriteLine($"{config.ChosenPackageManager} install failed with exit code {startInfoProcess.ExitCode}");
                if (!string.IsNullOrWhiteSpace(installError))
                {
                    Console.WriteLine($"Error output: {installError}");
                }
                if (!string.IsNullOrWhiteSpace(installOutput))
                {
                    Console.WriteLine($"Output: {installOutput}");
                }
                return false;
            }

            return true;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            Console.WriteLine($"Error: {config.ChosenPackageManager} is not installed or not found in PATH.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  1. Install {config.ChosenPackageManager} and ensure it's in your PATH");
            Console.WriteLine("  2. Restart the CLI and run setup to choose a different package manager");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during package installation: {ex.Message}");
            return false;
        }
    }

    private static async Task UpdatePackageJson(string projectPath, InitDetails initDetails)
    {
        var packageJsonPath = Path.Combine(projectPath, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            Console.WriteLine("Warning: package.json not found, skipping customization.");
            return;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(packageJsonPath);
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // Creates a new dictionary with all existing properties
            var packageData = new Dictionary<string, object?>();
            foreach (var property in root.EnumerateObject())
            {
                packageData[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
            }

            // Update with custom values if provided
            if (!string.IsNullOrWhiteSpace(initDetails.ProjectName))
                packageData["name"] = initDetails.ProjectName;

            if (!string.IsNullOrWhiteSpace(initDetails.Version))
                packageData["version"] = initDetails.Version;

            if (!string.IsNullOrWhiteSpace(initDetails.Description))
                packageData["description"] = initDetails.Description;

            if (!string.IsNullOrWhiteSpace(initDetails.EntryPoint))
                packageData["main"] = initDetails.EntryPoint;

            if (!string.IsNullOrWhiteSpace(initDetails.Author))
                packageData["author"] = initDetails.Author;

            // Write back with formatting
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(packageData, options);
            await File.WriteAllTextAsync(packageJsonPath, updatedJson);

            Console.WriteLine("package.json updated with custom details.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to update package.json: {ex.Message}");
        }
    }

    private static bool CanInitializeWithDetails(Config config)
    {
        return config.ChosenPackageManager == PackageManager.NPM
               || config.ChosenPackageManager == PackageManager.PNPM
               || config.ChosenPackageManager == PackageManager.YARN;
    }

    private static async Task<string> SelectDestination(ConfigurationService configService)
    {
        var pathResolver = new PathResolver(configService);

        while (true)
        {
            Console.WriteLine("\nYou can use placeholders like {Desktop}/myFolder or type a full path.");
            Console.WriteLine("Type 'paths' to see available placeholders, or 'exit' to cancel.\n");
            Console.Write("Enter destination folder path: ");

            var input = Console.ReadLine()?.Trim() ?? "";

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return "";

            if (input.Equals("paths", StringComparison.OrdinalIgnoreCase))
            {
                await pathResolver.ListPathsAsync();
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Path cannot be empty.");
                continue;
            }

            // Resolve placeholders to make sure paths are "legal" (no invalid placeholders)
            var (path, allResolved) = await pathResolver.ResolvePathWithStatusAsync(input);

            if (!allResolved)
            {
                Console.WriteLine("Please enter a valid path. Type 'paths' to see available placeholders.");
                continue; // Ask for path again
            }

            if (Directory.Exists(path)) return path;

            Console.Write($"Directory '{path}' does not exist. Create it? (y/n): ");
            var response = Console.ReadKey(true);
            Console.WriteLine();

            if (response.Key == ConsoleKey.Y)
            {
                try
                {
                    Directory.CreateDirectory(path);
                    Console.WriteLine($"Created directory: {path}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create directory: {ex.Message}");
                    continue;
                }
            }
            else
            {
                continue;
            }

            return path;
        }
    }

    private static string GetInstallPmCommand(Config config)
    {
        return config.ChosenPackageManager switch
        {
            PackageManager.NPM or PackageManager.PNPM => "install",
            PackageManager.YARN or PackageManager.BUN => "add",
            _ => throw new ArgumentException("Unknown package manager")
        };
    }

    private static string GetRealPackageNames(List<Packages> packages)
    {
        var realPackageNames = packages.Select(package =>
        {
            var match = PackageRegex().Match(package.Link);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }).ToList();

        return string.Join(" ", realPackageNames);
    }

    private static (string fileName, string arguments) GetShellCommand(string workingDirectory, string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return ("cmd.exe", $"/c cd /d \"{workingDirectory}\" && {command}");
        }

        // Linux/Mac - escape single quotes in path and command
        var escapedPath = workingDirectory.Replace("'", "'\\''");
        var escapedCommand = command.Replace("'", "'\\''");
        return ("/bin/sh", $"-c 'cd \"{escapedPath}\" && {escapedCommand}'");
    }

    private static string GetPackageManagerName(PackageManager packageManager)
    {
        return packageManager.ToString().ToLower();
    }

    // Match package name after /package/ or /packages/
    [GeneratedRegex(@"/packages?/([^/]+(?:/[^/]+)?)", RegexOptions.IgnoreCase, "en-gb")]
    private static partial Regex PackageRegex();
}