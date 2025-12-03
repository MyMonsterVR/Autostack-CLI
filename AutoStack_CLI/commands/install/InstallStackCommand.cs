using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.install;

public class InstallStackCommand(ApiClient api, ConfigurationService configurationService) : IEndpoint<InstallParameters, bool>, ICliCommand
{
    public string Name => "install";
    public string Description => "Installs a specified package";
    public string Usage => "install <id>";

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Stack ID is required");
            Console.WriteLine($"Usage: {Usage}");
            return;
        }

        if (!Guid.TryParse(args[0], out var stackId))
        {
            Console.WriteLine($"Error: Invalid stack ID format: {args[0]}");
            Console.WriteLine("Stack ID must be a valid GUID");
            return;
        }

        await ExecuteAsync(new InstallParameters(stackId));
    }
    
    public async Task<bool> ExecuteAsync(InstallParameters parameters)
    {
        var stack = await api.GetStackAsync(parameters.StackId);

        if (stack == null)
        {
            return false;
        }


        Console.WriteLine("Following packages will be installed:");
        Console.WriteLine("");
        foreach (var package in stack.Packages)
        {
            Console.WriteLine(FirstCharToUpper(package.Name));
        }
        
        Console.Write("Do you want to install the following packages? Y/n: ");
        var input = Console.ReadKey(true);
        if (input.Key == ConsoleKey.N)
        {
            return false;
        }
        
        Console.Clear();
        var verifiedPackages = stack.Packages.Where(p => p.IsVerified).ToList();
        var unverifiedPackages = stack.Packages.Where(p => !p.IsVerified).ToList();
        var installUnverifiedPackages = InstallUnverifiedPackages(unverifiedPackages);

        var packagesToInstall = new List<Packages>();
        packagesToInstall.AddRange(verifiedPackages);
        if(installUnverifiedPackages) packagesToInstall.AddRange(unverifiedPackages);

        var packageNames = string.Join(", ", packagesToInstall.Select(p => p.Name));

        Console.Clear();
        Console.WriteLine($"Following packages will be installed: {packageNames}");
        var installSuccess = await PackageManagerInstaller.InstallPackages(packagesToInstall, configurationService);

        if (installSuccess)
        {
            // Track download after successful installation
            // Try to get auth token for better rate limits
            var (_, authToken, _) = await configurationService.LoadCredentialsAsync();
            await api.TrackDownloadAsync(parameters.StackId, authToken);
            Console.WriteLine($"Successfully installed ${stack.Name}");
        }

        return installSuccess;
    }

    private static bool InstallUnverifiedPackages(List<Packages> unverifiedPackages)
    {
        if (unverifiedPackages.Count == 0) return false;

        Console.WriteLine($"Detected {unverifiedPackages.Count} unverified packages");
        foreach (var package in unverifiedPackages)
        {
            Console.WriteLine($"{FirstCharToUpper(package.Name)} - {package.Link}");
        }
        Console.WriteLine();
        Console.Write("Do you want to install the following packages (doing so is at your own risk)? y/N: ");
        var input = Console.ReadKey(true);
        return input.Key == ConsoleKey.Y;
    }
    
    private static string FirstCharToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        input = input.ToLower();
        return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
    }
}