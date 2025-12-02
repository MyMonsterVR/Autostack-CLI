using AutoStack_CLI.Commands;

namespace AutoStack_CLI.services;

public class ProtocolHandler(CommandHandler commandHandler)
{
    public async Task<bool> HandleProtocolAsync(string uri)
    {
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
            "install"  when parts.Length > 1 => await HandleInstallStackAsync(parts[1]),
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
        Console.ReadKey();
        return true;

    }
    
    private async Task<bool> HandleInstallStackAsync(string stackIdStr)
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

        Console.WriteLine("");
        Console.WriteLine("Do you want to install this stack? Y/n");
        var input = Console.ReadKey(intercept: true);

        if (input.Key != ConsoleKey.Y) return false;

        Console.WriteLine($"Installing {stack.Name} by {stack.Username}");
        await commandHandler.ExecuteInstallStackAsync(stackId);
        
        Console.ReadKey();
        return true;

    }

    private static bool HandleUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        return false;
    }
}
