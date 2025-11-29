using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.findstack;

public class GetStackCommand(ApiClient api) : IEndpoint<GetStackParameters, Stack>, ICliCommand
{
    public string Name => "getstack";
    public string Description => "Get information about a stack";
    public string Usage => "getstack <id>";

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

        await ExecuteAsync(new GetStackParameters(stackId));
    }
    
    public async Task<Stack?> ExecuteAsync(GetStackParameters parameters)
    {
        var stack = await api.GetStackAsync(parameters.StackId);

        if (stack != null)
        {
            Console.WriteLine($"Stack: {stack.Name}");
            Console.WriteLine($"Description: {stack.Description}");
            Console.WriteLine($"Downloads: {stack.Downloads}");
            Console.WriteLine($"Created by: {stack.Username}");
            Console.WriteLine($"Packages: {string.Join("\n", stack.Packages?.Select(p => p.PackageName) ?? [])}");

            return stack;
        }

        Console.WriteLine("Stack not found");

        return null;
    }
}