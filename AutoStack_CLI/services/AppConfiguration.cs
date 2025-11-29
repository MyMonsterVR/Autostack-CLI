using Microsoft.Extensions.Configuration;

namespace AutoStack_CLI.services;

public class AppConfiguration
{
    public string ApiBaseUrl { get; }
    public string UpdateCheckUrl { get; }
    public string UpdateDownloadUrlWindows { get; }
    public string UpdateDownloadUrlLinux { get; }
    public bool IsDebugMode { get; }

    public AppConfiguration()
    {
        // Determine environment
#if DEBUG
        IsDebugMode = true;
        var environment = "Development";
#else
        IsDebugMode = false;
        var environment = "Production";
#endif

        // Build configuration from JSON files
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Load settings from configuration
        ApiBaseUrl = configuration["AppSettings:ApiBaseUrl"]
            ?? throw new InvalidOperationException("ApiBaseUrl is not configured");
        UpdateCheckUrl = configuration["AppSettings:UpdateCheckUrl"]
            ?? throw new InvalidOperationException("UpdateCheckUrl is not configured");
        UpdateDownloadUrlWindows = configuration["AppSettings:UpdateDownloadUrlWindows"]
            ?? throw new InvalidOperationException("UpdateDownloadUrlWindows is not configured");
        UpdateDownloadUrlLinux = configuration["AppSettings:UpdateDownloadUrlLinux"]
            ?? throw new InvalidOperationException("UpdateDownloadUrlLinux is not configured");

        if (IsDebugMode)
        {
            Console.WriteLine($"[DEBUG MODE] Using API at {ApiBaseUrl}");
        }
    }
}
