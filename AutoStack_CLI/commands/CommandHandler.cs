using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.Commands.login;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands;

public class CommandHandler(ApiClient apiClient)
{
    public async Task<Stack?> ExecuteGetStackAsync(Guid stackId)
    {
        var command = new GetStackCommand(apiClient);
        var stack = await command.ExecuteAsync(new GetStackParameters(stackId));
        return stack;
    }

    public async Task ExecuteLoginAsync(string username, string password)
    {
        var command = new LoginCommand(apiClient);
        await command.ExecuteAsync(new LoginParameters(username, password));
    }
}