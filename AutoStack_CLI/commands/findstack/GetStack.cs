using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.findstack;

public class GetStack : IEndpoint<GetStackParameters, Stack>
{
    private readonly ApiClient _api;

    public GetStack(ApiClient api)
    {
        _api = api;
    }

    public async Task<Stack?> ExecuteAsync(GetStackParameters parameters)
    {
        var stack = await _api.GetStackAsync(parameters.StackId);

        if (stack != null)
        {
            Console.WriteLine($"Stack: {stack.Name}");
            Console.WriteLine($"Description: {stack.Description}");
            Console.WriteLine($"Downloads: {stack.Downloads}");
            Console.WriteLine($"Created by: {stack.Username}");
            Console.WriteLine($"Packages: {string.Join(", ", stack.Packages?.Select(p => p.PackageName) ?? [])}");

            return stack;
        }

        Console.WriteLine("Stack not found");

        return null;
    }
}