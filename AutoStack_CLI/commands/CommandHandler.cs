using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.Commands.login;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands;

public class CommandHandler
{
    private readonly ApiClient _apiClient;

    public CommandHandler(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteGetStackAsync(Guid stackId)
    {
        var command = new GetStack(_apiClient);
        await command.ExecuteAsync(new GetStackParameters(stackId));
    }

    public async Task ExecuteLoginAsync(string username, string password)
    {
        var command = new Login(_apiClient);
        await command.ExecuteAsync(new LoginParameters(username, password));
    }
}