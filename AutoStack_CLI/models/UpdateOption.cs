namespace AutoStack_CLI.models;

public class UpdateOption
{
    public string Display { get; }
    public bool ShouldUpdate { get; }

    public UpdateOption(string display, bool shouldUpdate)
    {
        Display = display;
        ShouldUpdate = shouldUpdate;
    }
}
