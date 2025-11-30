using AutoStack_CLI.Commands;
using AutoStack_CLI.Commands.config;
using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.Commands.install;
using AutoStack_CLI.Commands.login;
using AutoStack_CLI.services;

var config = new ApiConfiguration();

// Initialize services
var startup = new StartupService(config);
await startup.InitializeAsync(args);

var apiClient = new ApiClient(config);
var configService = new ConfigurationService();

// Commands
var commandHandler = new CommandHandler(apiClient, configService);
var protocolHandler = new ProtocolHandler(commandHandler);


// CLI command registry
var registry = new CliCommandRegistry();
var loginCommand = new LoginCommand(apiClient, configService);
var installStackCommand = new InstallStackCommand(apiClient, configService);

registry.Register(new GetStacksCommand(apiClient, installStackCommand));
registry.Register(installStackCommand);
registry.Register(loginCommand);
registry.Register(new SetupCommand(configService, loginCommand));
registry.Register(new PathsCommand(configService));

Console.Title = "AutoStack CLI";

var configFile = configService.ConfigExists();
if (!configFile)
{
    await registry.ExecuteAsync("setup");
}


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
