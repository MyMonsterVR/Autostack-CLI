using AutoStack_CLI.Commands.login;
using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.config;

public class SetupCommand(ConfigurationService configurationService) : IEndpoint, ICliCommand
{
    private bool _packageManagerInstalled = false;
    
    public string Name => "setup";
    public string Description => "Runs the setup script";
    public string Usage => "setup";
    public async Task ExecuteAsync(string[] args)
    {
        var packages = Enum.GetValues<PackageManager>().ToList();
        // Removes NOTCHOSEN option
        packages.RemoveAt(0);

        var chosenPackageManager = PackageManager.NOTCHOSEN;

        while (!_packageManagerInstalled)
        {
            var installStatus = new Dictionary<PackageManager, bool>();
            foreach (var pm in packages)
            {
                installStatus[pm] = await PackageManagerDetector.IsInstalledAsync(pm);
            }

            var menu = new InteractiveMenu<PackageManager>(
                items: packages,
                displaySelector: pm => $"{pm,-10} - {(installStatus[pm] ? "Installed" : "Not Installed")}",
                title: "Welcome to AutoStack CLI!\nWhat package manager do you want to use?\nIf your package manager is showing as \"Not Installed\" then it might not be added to your path"
            );

            chosenPackageManager = await menu.ShowAsync();
            if (await PackageManagerDetector.IsInstalledAsync(chosenPackageManager))
            {
                _packageManagerInstalled = true;
            }
            else
            {
                Console.WriteLine("Package manager not installed");
                Console.WriteLine("Press any key to re-pick your package manager.");
                Console.ReadKey(true);
            }
        }
        Console.Clear();
        
        if (!configurationService.CredentialsExist())
        {
            Console.WriteLine("Do you want to login? y/N");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y)
            {
                Console.WriteLine("Login...");
            }
        }

        var config = new Config
        {
            ChosenPackageManager = chosenPackageManager
        };
        
        await configurationService.SavePreferencesAsync(config);
    }
}