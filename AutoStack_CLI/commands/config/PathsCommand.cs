using AutoStack_CLI.interfaces;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.config;

/// <summary>
/// Command for managing custom path placeholders
/// </summary>
public class PathsCommand(ConfigurationService configService) : ICliCommand
{
    public string Name => "paths";
    public string Description => "Manage custom path placeholders";
    public string Usage => "paths [list|add|remove] [name] [path]";

    public async Task ExecuteAsync(string[] args)
    {
        var pathResolver = new PathResolver(configService);

        if (args.Length == 0)
        {
            await ShowHelp(pathResolver);
            return;
        }

        var action = args[0].ToLower();

        switch (action)
        {
            case "list":
            case "ls":
                await pathResolver.ListPathsAsync();
                break;

            case "add":
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: paths add <name> <path>");
                    Console.WriteLine("Example: paths add Projects {Desktop}/MyProjects");
                    return;
                }
                var name = args[1];
                var path = string.Join(" ", args.Skip(2)); // Allow paths with spaces
                await pathResolver.AddCustomPathAsync(name, path);
                break;

            case "remove":
            case "rm":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: paths remove <name>");
                    Console.WriteLine("Example: paths remove Projects");
                    return;
                }
                await pathResolver.RemoveCustomPathAsync(args[1]);
                break;

            case "help":
            case "-h":
            case "--help":
                await ShowHelp(pathResolver);
                break;

            default:
                Console.WriteLine($"Unknown action: {action}");
                await ShowHelp(pathResolver);
                break;
        }
    }

    private static async Task ShowHelp(PathResolver pathResolver)
    {
        Console.WriteLine("\nPath Placeholder Management");
        Console.WriteLine("===========================");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  paths list              - List all available path placeholders");
        Console.WriteLine("  paths add <name> <path> - Add a custom path placeholder");
        Console.WriteLine("  paths remove <name>     - Remove a custom path placeholder");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  paths add Projects {Desktop}/MyProjects");
        Console.WriteLine("  paths add Work C:/Users/John/WorkFolder");
        Console.WriteLine("  paths remove Projects");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  Use {PlaceholderName} in paths, e.g., {Desktop}/myFolder");
        Console.WriteLine("  Custom paths can reference other placeholders");

        Console.WriteLine();
        await pathResolver.ListPathsAsync();
    }
}
