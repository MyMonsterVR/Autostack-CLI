using AutoStack_CLI.Commands;

namespace AutoStack_CLI.services;

public class ProtocolHandler(CommandHandler commandHandler)
{
    public async Task<bool> HandleProtocolAsync(string uri)
    {
        Console.WriteLine($"AutoStack launched with: {uri}");

        if (!uri.StartsWith("autostack://"))
        {
            return false;
        }

        var path = uri.Substring("autostack://".Length);
        var parts = path.Split('/');
        var command = parts[0];

        return command switch
        {
            "getstack" when parts.Length > 1 => await HandleGetStackAsync(parts[1]),
            "login" => HandleLogin(),
            _ => HandleUnknownCommand(command)
        };
    }

    private async Task<bool> HandleGetStackAsync(string stackIdStr)
    {
        if (!Guid.TryParse(stackIdStr, out var stackId))
        {
            Console.WriteLine("Invalid stack ID format");
            return false;
        }

        var stack = await commandHandler.ExecuteGetStackAsync(stackId);
        if (stack == null)
        {
            return false;
        }

        Console.WriteLine("Do you want to install this stack? Y/n");
        var input = Console.ReadKey(intercept: true);

        if (input.Key != ConsoleKey.Y) return false;

        Console.WriteLine($"Installing {stack.Name} by {stack.Username}");
        Console.ReadKey();
        return true;

    }

    private bool HandleLogin()
    {
        Console.WriteLine("Login command received");
        return true;
    }

    private bool HandleUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        return false;
    }
}
