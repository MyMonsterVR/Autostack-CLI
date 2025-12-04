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

        var verifiedPackages = stack.Packages.Where(p => p.IsVerified).ToList();
        var unverifiedPackages = stack.Packages.Where(p => !p.IsVerified).ToList();

        // Build title with package list
        var title = BuildPackageListTitle(stack.Name, verifiedPackages, unverifiedPackages);

        // Create install options
        var options = new List<InstallOption>();

        if (verifiedPackages.Count > 0 && unverifiedPackages.Count > 0)
        {
            options.Add(new InstallOption("Install All", InstallChoice.InstallAll));
            options.Add(new InstallOption("Install Verified Only", InstallChoice.VerifiedOnly));
        }
        else if (verifiedPackages.Count > 0)
        {
            options.Add(new InstallOption("Install", InstallChoice.InstallAll));
        }
        else if (unverifiedPackages.Count > 0)
        {
            options.Add(new InstallOption("Install (Unverified - at your own risk)", InstallChoice.InstallAll));
        }

        options.Add(new InstallOption("Cancel", InstallChoice.Cancel));

        // Show interactive menu
        var menu = new InteractiveMenu<InstallOption>(
            items: options,
            displaySelector: option => option.Display,
            title: title
        );

        var selectedOption = await menu.ShowAsync();

        if (selectedOption == null || selectedOption.Choice == InstallChoice.Cancel)
        {
            Console.Clear();
            Console.WriteLine("Installation cancelled.");
            return false;
        }

        // Determine which packages to install
        var packagesToInstall = new List<Packages>();
        if (selectedOption.Choice == InstallChoice.InstallAll)
        {
            packagesToInstall.AddRange(verifiedPackages);
            packagesToInstall.AddRange(unverifiedPackages);
        }
        else if (selectedOption.Choice == InstallChoice.VerifiedOnly)
        {
            packagesToInstall.AddRange(verifiedPackages);
        }

        Console.Clear();
        var installSuccess = await PackageManagerInstaller.InstallPackages(packagesToInstall, configurationService);

        if (installSuccess)
        {
            // Track download after successful installation
            // Try to get auth token for better rate limits
            var (_, authToken, _) = await configurationService.LoadCredentialsAsync();
            await api.TrackDownloadAsync(parameters.StackId, authToken);
            Console.WriteLine($"Successfully installed {stack.Name}");
        }

        return installSuccess;
    }

    private static string BuildPackageListTitle(string stackName, List<Packages> verifiedPackages, List<Packages> unverifiedPackages)
    {
        var title = $"Stack: {stackName}\n";

        if (verifiedPackages.Count > 0)
        {
            title += "\nVerified packages:\n";
            foreach (var package in verifiedPackages)
            {
                title += $"  - {FirstCharToUpper(package.Name)}\n";
            }
        }

        if (unverifiedPackages.Count > 0)
        {
            title += $"\nUnverified packages ({unverifiedPackages.Count}):\n";
            foreach (var package in unverifiedPackages)
            {
                title += $"  - {FirstCharToUpper(package.Name)} ({package.Link})\n";
            }
            title += "\nWARNING: Unverified packages have not been reviewed. Install at your own risk.\n";
        }

        return title;
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