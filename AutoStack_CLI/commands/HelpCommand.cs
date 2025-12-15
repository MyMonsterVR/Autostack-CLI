using AutoStack_CLI.interfaces;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands;

/// <summary>
/// Command for displaying help information about available commands
/// </summary>
public class HelpCommand(CliCommandRegistry registry) : ICliCommand
{
    public string Name => "help";
    public string Description => "Display available commands and their usage";
    public string Usage => "help [command]";

    public Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowGeneralHelp();
        }
        else
        {
            ShowCommandHelp(args[0]);
        }

        return Task.CompletedTask;
    }

    private void ShowGeneralHelp()
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          AutoStack CLI                                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("AutoStack helps you quickly install curated technology stacks.");
        Console.WriteLine();
        Console.WriteLine("Available Commands:");
        Console.WriteLine("═══════════════════");
        Console.WriteLine();

        foreach (var command in registry.GetAllCommands().OrderBy(c => c.Name))
        {
            Console.WriteLine($"  {command.Usage,-25} {command.Description}");
        }

        Console.WriteLine();
        Console.WriteLine("Tips:");
        Console.WriteLine("  • Type 'help <command>' for detailed help on a specific command");
        Console.WriteLine("  • Use arrow keys to navigate interactive menus");
        Console.WriteLine("  • Press ESC to cancel any interactive menu");
        Console.WriteLine();
    }

    private void ShowCommandHelp(string commandName)
    {
        var command = registry.GetAllCommands()
            .FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        if (command == null)
        {
            Console.WriteLine($"Unknown command: {commandName}");
            Console.WriteLine("Type 'help' to see all available commands.");
            return;
        }

        Console.Clear();
        Console.WriteLine($"Command: {command.Name}");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"Description: {command.Description}");
        Console.WriteLine();
        Console.WriteLine($"Usage: {command.Usage}");
        Console.WriteLine();

        // Add specific examples for common commands
        ShowCommandExamples(command.Name);
    }

    private static void ShowCommandExamples(string commandName)
    {
        switch (commandName.ToLower())
        {
            case "install":
                Console.WriteLine("Examples:");
                Console.WriteLine("  install 123e4567-e89b-12d3-a456-426614174000");
                Console.WriteLine();
                Console.WriteLine("This will:");
                Console.WriteLine("  1. Fetch the stack from AutoStack");
                Console.WriteLine("  2. Display all packages in the stack");
                Console.WriteLine("  3. Let you choose to install all or only verified packages");
                Console.WriteLine("  4. Install packages to a directory of your choice");
                break;

            case "getstacks":
                Console.WriteLine("Examples:");
                Console.WriteLine("  getstacks");
                Console.WriteLine();
                Console.WriteLine("This will:");
                Console.WriteLine("  1. Show an interactive list of available stacks");
                Console.WriteLine("  2. Navigate with arrow keys (left/right for pages)");
                Console.WriteLine("  3. Press Enter to select a stack");
                Console.WriteLine("  4. Choose to view details or install");
                break;

            case "setup":
                Console.WriteLine("Examples:");
                Console.WriteLine("  setup");
                Console.WriteLine();
                Console.WriteLine("This will:");
                Console.WriteLine("  1. Detect installed package managers");
                Console.WriteLine("  2. Let you select your preferred package manager");
                Console.WriteLine("  3. Optionally prompt for login credentials");
                break;

            case "paths":
                Console.WriteLine("Examples:");
                Console.WriteLine("  paths list                           - List all path placeholders");
                Console.WriteLine("  paths add Projects {Desktop}/MyCode  - Add custom placeholder");
                Console.WriteLine("  paths remove Projects                - Remove placeholder");
                Console.WriteLine();
                Console.WriteLine("Path placeholders let you use shortcuts like {Desktop} or {Documents}");
                Console.WriteLine("when choosing installation directories.");
                break;

            case "login":
                Console.WriteLine("Examples:");
                Console.WriteLine("  login");
                Console.WriteLine();
                Console.WriteLine("This will:");
                Console.WriteLine("  1. Prompt for your AutoStack username");
                Console.WriteLine("  2. Prompt for your password (hidden)");
                Console.WriteLine("  3. Save encrypted credentials locally");
                break;

            case "logout":
                Console.WriteLine("Examples:");
                Console.WriteLine("  logout");
                Console.WriteLine();
                Console.WriteLine("This will remove your saved credentials from this machine.");
                break;
        }
    }
}
