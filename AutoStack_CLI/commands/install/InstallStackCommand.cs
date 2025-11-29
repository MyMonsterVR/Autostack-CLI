using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.install;

public class InstallStackCommand(ApiClient api) : IEndpoint<InstallParameters, bool>, ICliCommand
{
    public string Name => "install";
    public string Description => "Installs a specified package";
    public string Usage => "install <id>";

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Stack ID is required");
            Console.WriteLine($"Usage: {Usage}");
            return;
        }

        if (!Guid.TryParse(args[0], out var stackId))
        {
            Console.WriteLine($"Error: Invalid stack ID format: {args[0]}");
            Console.WriteLine("Stack ID must be a valid GUID");
            return;
        }

        await ExecuteAsync(new InstallParameters(stackId));
    }
    
    public async Task<bool> ExecuteAsync(InstallParameters parameters)
    {
        var stack = await api.GetStackAsync(parameters.StackId);

        if (stack == null)
        {
            return false;
        }

        Console.WriteLine("Do you want to install the following packages?");

        foreach (var package in stack.Packages)
        {
            Console.WriteLine(FirstCharToUpper(package.PackageName));
        }
        
        Console.WriteLine();
        var input = Console.ReadKey(true);
        if (input.Key == ConsoleKey.Y)
        {
            Console.WriteLine($"Installing {stack.Name}...");
            var verifiedPackages = stack.Packages.Where(p => p.IsVerified).ToList();
            var unverifiedPackages = stack.Packages.Where(p => !p.IsVerified).ToList();
            var installUnverifiedPackages = false;

            if (unverifiedPackages.Count == 0)
            {
                Console.WriteLine($"Detected {unverifiedPackages.Count} unverified packages. Do you want to install the following packages? Y/n");
                input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Y)
                {
                    foreach (var package in unverifiedPackages)
                    {
                        Console.WriteLine(FirstCharToUpper(package.PackageName));
                    }
                }
                else
                {
                    installUnverifiedPackages = true;
                }
            }
        }

        return true;
    }
    
    static string FirstCharToUpper(string input)
    {
        input = input.ToLower();
        return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
    }
}