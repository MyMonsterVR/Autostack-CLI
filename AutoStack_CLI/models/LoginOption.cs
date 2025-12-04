namespace AutoStack_CLI.models;

public class LoginOption
{
    public string Display { get; }
    public bool ShouldLogin { get; }

    public LoginOption(string display, bool shouldLogin)
    {
        Display = display;
        ShouldLogin = shouldLogin;
    }
}
