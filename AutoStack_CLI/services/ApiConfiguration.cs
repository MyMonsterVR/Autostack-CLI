using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace AutoStack_CLI.services;

public class ApiConfiguration
{
    public string ApiBaseUrl { get; }
    public string UpdateCheckUrl { get; }
    public string UpdateDownloadUrlWindows { get; }
    public string UpdateDownloadUrlLinux { get; }
    public bool IsDebugMode { get; }

    public ApiConfiguration()
    {
        // Determine environment
#if DEBUG
        IsDebugMode = true;
        var environment = "Development";
#else
        IsDebugMode = false;
        var environment = "Production";
#endif

        // Build configuration from JSON files or embedded resources
        var configBuilder = new ConfigurationBuilder();

#if DEBUG
        configBuilder
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
#else
        // Release: Read from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "AutoStack_CLI.appsettings.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");

        configBuilder.AddJsonStream(stream);
#endif

        var configuration = configBuilder
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
