using AutoStack_CLI.Commands.install;
using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.findstack;

public class GetStacksCommand(ApiClient api, InstallStackCommand installStackCommand) : IEndpoint, ICliCommand
{
    public string Name => "getstacks";
    public string Description => "Get all stacks";
    public string Usage => "getstacks";

    public async Task ExecuteAsync(string[] args)
    {
        var itemsPerPage = 10;
        var pageNumber = 1;
        var stacks = await api.GetStacksAsync(pageNumber, itemsPerPage);

        if (stacks.Items.Count == 0)
        {
            Console.WriteLine("No stacks found");
            return;
        }

        var menu = new InteractiveMenu<Stack>(
            items: stacks.Items,
            displaySelector: stack =>
                $"Stack: {stack.Name} - Type: {Enum.Parse<StackType>(stack.Type)} - Downloads: {stack.Downloads} - Id: {stack.Id}",
            itemsPerPage: itemsPerPage,
            totalPages: stacks.TotalPages,
            onPageChange: async newPageNumber =>
            {
                var stack = await api.GetStacksAsync(newPageNumber, itemsPerPage);
                return stack.Items;
            }
        );

        var chosenStack = await menu.ShowAsync();
        Console.Clear();
        Console.WriteLine($"You chose: {chosenStack.Name} - Type: {Enum.Parse<StackType>(chosenStack.Type)}");
        var stackParams = new InstallParameters(chosenStack.Id);
        await installStackCommand.ExecuteAsync(stackParams);
    }
}