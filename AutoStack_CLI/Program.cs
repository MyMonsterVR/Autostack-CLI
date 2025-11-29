using AutoStack_CLI.Commands;
using AutoStack_CLI.services;

var config = new AppConfiguration();

// Initialize services
var startup = new StartupService(config);
await startup.InitializeAsync();

var apiClient = new ApiClient(config);
var commandHandler = new CommandHandler(apiClient);
var protocolHandler = new ProtocolHandler(commandHandler);

// Handle command line arguments
if (args.Length > 0)
{
    await protocolHandler.HandleProtocolAsync(args[0]);
}
else
{
    Console.WriteLine("AutoStack CLI - No arguments provided");
}

Console.ReadKey();
