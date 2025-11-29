using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.findstack;

public class GetStacksCommand(ApiClient api) : IEndpoint, ICliCommand
{
    public string Name => "getstacks";
    public string Description => "Get all stacks";
    public string Usage => "getstacks";

    public async Task ExecuteAsync(string[] args)
    {
        var stacks = await api.GetStacksAsync();

        if (stacks == null || stacks.Count == 0)
        {
            Console.WriteLine("No stacks found");
            return;
        }

        foreach (var stack in stacks)
        {
            if (stack != null)
            {
                Console.WriteLine($"Stack: {stack.Name} - Type: {Enum.Parse<StackType>(stack.Type)} - Downloads: {stack.Downloads} - Id: {stack.Id}");
            }
        }
    }
}