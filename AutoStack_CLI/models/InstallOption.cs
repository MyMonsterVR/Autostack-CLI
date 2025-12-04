namespace AutoStack_CLI.models;

public class InstallOption
{
    public string Display { get; }
    public InstallChoice Choice { get; }

    public InstallOption(string display, InstallChoice choice)
    {
        Display = display;
        Choice = choice;
    }
}
