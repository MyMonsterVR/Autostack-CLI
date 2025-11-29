using AutoStack_CLI.Commands;
using AutoStack_CLI.Commands.install;
using AutoStack_CLI.Commands.login;
using AutoStack_CLI.services;

var config = new AppConfiguration();

// Initialize services
var startup = new StartupService(config);
await startup.InitializeAsync();

var apiClient = new ApiClient(config);
var commandHandler = new CommandHandler(apiClient);
var protocolHandler = new ProtocolHandler(commandHandler);

// Setup CLI command registry
var registry = new CliCommandRegistry();
registry.Register(new InstallCommand(apiClient));
registry.Register(new LoginCommand(apiClient));

// Handle command line arguments
if (args.Length > 0)
{
    await protocolHandler.HandleProtocolAsync(args[0]);
    Console.ReadKey();
}
else
{
    // Interactive mode - loop until user exits
    registry.ShowHelp();

    while (true)
    {
        Console.WriteLine();
        Console.Write("Command: ");
        var command = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(command))
        {
            await registry.ExecuteAsync(command);
        }
        else
        {
            Console.WriteLine("Please enter a command. Type 'help' to see available commands.");
        }
    }
}
