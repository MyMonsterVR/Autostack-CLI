namespace AutoStack_CLI.services;

public class StartupService(ApiConfiguration config)
{
    private readonly UpdateService updateService = new(config);

    public async Task InitializeAsync(string[] args)
    {
        await CheckForUpdatesAsync(args);
    }

    private async Task CheckForUpdatesAsync(string[] args)
    {
        try
        {
            await updateService.CheckForUpdatesAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update check failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
