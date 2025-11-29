using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.login;

public class LoginCommand(ApiClient api, ConfigurationService configService) : IEndpoint<LoginParameters, Token>, ICliCommand
{
    public string Name => "login";
    public string Description => "Login to AutoStack";
    public string Usage => "login <username> <password>";

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Username and password are required");
            Console.WriteLine($"Usage: {Usage}");
            return;
        }

        var username = args[0];
        var password = args[1];

        await ExecuteAsync(new LoginParameters(username, password));
    }

    public async Task<Token?> ExecuteAsync(LoginParameters parameters)
    {
        var result = await api.LoginAsync(parameters.Username, parameters.Password);

        if (result != null)
        {
            var config = await configService.LoadConfigAsync();
            
            // Save credentials securely
            await configService.SaveConfigAsync(
                parameters.Username,
                result.AccessToken,
                config.ChosenPackageManager
            );

            Console.WriteLine("Login successful!");
            return result;
        }

        Console.WriteLine("Login failed!");

        return null;
    }
}