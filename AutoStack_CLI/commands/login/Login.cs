using AutoStack_CLI.interfaces;
using AutoStack_CLI.models;
using AutoStack_CLI.models.parameters;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.login;

public class Login : IEndpoint<LoginParameters, Token>
{
    private readonly ApiClient _api;

    public Login(ApiClient api)
    {
        _api = api;
    }

    public async Task<Token?> ExecuteAsync(LoginParameters parameters)
    {
        var result = await _api.LoginAsync(parameters.Username, parameters.Password);

        if (result != null)
        {
            Console.WriteLine("Login successful!");
            return result;
        }

        Console.WriteLine("Login failed!");

        return null;
    }
}