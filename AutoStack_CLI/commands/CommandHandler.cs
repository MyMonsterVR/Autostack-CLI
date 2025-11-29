using AutoStack_CLI.Commands.findstack;
using AutoStack_CLI.Commands.install;
using AutoStack_CLI.Commands.login;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands;

public class CommandHandler(ApiClient apiClient, ConfigurationService config)
{
    private readonly ApiClient _apiClient = apiClient;
    private readonly ConfigurationService _config = config;
    
    public async Task<Stack?> ExecuteGetStackAsync(Guid stackId)
    {
        Console.Clear();

        var command = new GetStackCommand(_apiClient);
        var stack = await command.ExecuteAsync(new GetStackParameters(stackId));
        return stack;
    }
    
    public async Task ExecuteInstallStackAsync(Guid stackId)
    {
        var command = new InstallStackCommand(_apiClient);
        await command.ExecuteAsync(new InstallParameters(stackId));
    }

    public async Task ExecuteLoginAsync(string username, string password)
    {
        var command = new LoginCommand(_apiClient, _config);
        await command.ExecuteAsync(new LoginParameters(username, password));
    }
}