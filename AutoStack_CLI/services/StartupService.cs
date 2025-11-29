namespace AutoStack_CLI.services;

public class StartupService(AppConfiguration config)
{
    private readonly UpdateService updateService = new(config);

    public async Task InitializeAsync()
    {
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await updateService.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update check failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
