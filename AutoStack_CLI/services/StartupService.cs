namespace AutoStack_CLI.services;

public class StartupService
{
    private readonly UpdateService _updateService;

    public StartupService(AppConfiguration config)
    {
        _updateService = new UpdateService(config);
    }

    public async Task InitializeAsync()
    {
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _updateService.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update check failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
