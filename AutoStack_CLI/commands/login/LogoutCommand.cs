using AutoStack_CLI.interfaces;
using AutoStack_CLI.services;

namespace AutoStack_CLI.Commands.login;

public class LogoutCommand(ConfigurationService configService) : ICliCommand
{
    public string Name => "logout";
    public string Description => "Logout and clear saved credentials";
    public string Usage => "logout";

    public async Task ExecuteAsync(string[] args)
    {
        var isLoggedIn = await configService.IsLoggedInAsync();

        if (!isLoggedIn)
        {
            Console.WriteLine("You are not currently logged in.");
            return;
        }

        configService.ClearConfig();
        Console.WriteLine("Logged out successfully. All credentials have been cleared.");
    }
}
