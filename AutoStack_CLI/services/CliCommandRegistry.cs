using AutoStack_CLI.interfaces;

namespace AutoStack_CLI.services;

public class CliCommandRegistry
{
    private readonly Dictionary<string, ICliCommand> _commands = new();

    public void Register(ICliCommand command)
    {
        _commands[command.Name.ToLower()] = command;
    }

    public async Task ExecuteAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1..] : [];

        if (_commands.TryGetValue(commandName, out var command))
        {
            await command.ExecuteAsync(args);
            return;
        }

        Console.WriteLine($"Unknown command: {commandName}");
        Console.WriteLine("Type 'help' to see available commands.");
    }

    public void ShowHelp()
    {
        Console.WriteLine("AutoStack CLI");
        Console.WriteLine();
        Console.WriteLine("Available Commands:");
        Console.WriteLine();

        foreach (var command in _commands.Values.OrderBy(c => c.Name))
        {
            Console.WriteLine($"  {command.Usage,-20} - {command.Description}");
        }

        Console.WriteLine();
    }

    public IEnumerable<ICliCommand> GetAllCommands() => _commands.Values;
}
