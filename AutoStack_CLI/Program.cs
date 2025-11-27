using AutoStack_CLI.Commands;
using AutoStack_CLI.services;

// Check for updates on startup
var updateService = new UpdateService();
await updateService.CheckForUpdatesAsync();

ApiClient apiClient = new ApiClient();
CommandHandler commandHandler = new(apiClient);

if (args.Length > 0)
{
    var uri = args[0];
    Console.WriteLine($"AutoStack launched with: {uri}");

    // Parse the URI (e.g., autostack://getstack/94833069-e44a-4375-8c15-3c51a79746f8)
    if (uri.StartsWith("autostack://"))
    {
        var path = uri.Substring("autostack://".Length);
        var parts = path.Split('/');
        var command = parts[0];

        if (command == "getstack" && parts.Length > 1)
        {
            if (Guid.TryParse(parts[1], out var stackId))
            {
                var stack = await commandHandler.ExecuteGetStackAsync(stackId);
                if (stack != null)
                {
                    Console.WriteLine("Do you want to install this stack? Y/n");
                    var input = Console.ReadKey();
                    if (input.Key == ConsoleKey.Y)
                    {
                        // install
                        Console.WriteLine($"Installing {stack.Name} by {stack.Username}");
                        Console.ReadKey();
                        return;
                    }
                }

                return;
            }

            Console.WriteLine("Invalid stack ID format");
        }
        else if (command == "login")
        {
            // For login, you might prompt for username/password or parse from URI
            Console.WriteLine("Login command received");
        }
        else
        {
            Console.WriteLine($"Unknown command: {command}");
        }
    }
}
else
{
    Console.WriteLine("AutoStack CLI - No arguments provided");
}

Console.ReadKey();
