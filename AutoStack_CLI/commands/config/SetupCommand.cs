using AutoStack_CLI.Commands.login;
using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.config;

public class SetupCommand(ConfigurationService configurationService, LoginCommand loginCommand) : IEndpoint, ICliCommand
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

            // Check if ANY package manager is installed
            var anyInstalled = installStatus.Values.Any(x => x);

            if (!anyInstalled)
            {
                Console.WriteLine("No package managers detected!");
                Console.WriteLine();
                Console.WriteLine("AutoStack requires at least one package manager to be installed:");
                Console.WriteLine("  - NPM  : https://nodejs.org/");
                Console.WriteLine("  - PNPM : https://pnpm.io/");
                Console.WriteLine("  - YARN : https://yarnpkg.com/");
                Console.WriteLine("  - BUN  : https://bun.sh/");
                Console.WriteLine();
                Console.WriteLine("After installing one, restart the CLI to run setup again.");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
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
                Console.Clear();
                Console.WriteLine($"{chosenPackageManager} is not installed or not found in your PATH.");
                Console.WriteLine();
                Console.WriteLine("Please install the package manager or ensure it's added to your PATH.");
                Console.WriteLine("Press any key to select a different package manager.");
                Console.ReadKey(true);
            }
        }
        Console.Clear();

        await CheckCredentialsAsync();

        var config = new Config
        {
            ChosenPackageManager = chosenPackageManager
        };
        
        await configurationService.SavePreferencesAsync(config);
    }

    private async Task CheckCredentialsAsync()
    {
        if (!configurationService.CredentialsExist())
        {
            var options = new List<LoginOption>
            {
                new LoginOption("Login now", true),
                new LoginOption("Skip login", false)
            };

            var menu = new InteractiveMenu<LoginOption>(
                items: options,
                displaySelector: option => option.Display,
                title: "Would you like to login to your AutoStack account?"
            );

            var selectedOption = await menu.ShowAsync();

            if (selectedOption?.ShouldLogin == true)
            {
                Console.Clear();
                Console.Write("Username: ");
                var username = Console.ReadLine();

                Console.Write("Password: ");
                var password = ReadPassword();
                Console.WriteLine();

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await loginCommand.ExecuteAsync([username, password]);
                }
                else
                {
                    Console.WriteLine("Invalid username or password");
                }
            }
            else
            {
                Console.Clear();
            }
        }
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[0..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                password += keyInfo.KeyChar;
                Console.Write("*");
            }
        } while (key != ConsoleKey.Enter);

        return password;
    }
}