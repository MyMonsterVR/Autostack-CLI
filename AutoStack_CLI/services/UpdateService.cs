using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace AutoStack_CLI.services;

public class UpdateService
{
    private readonly HttpClient _httpClient = new();
    private const string UpdateCheckUrl = "https://autostack.dk/api/cli/version";
    private const string UpdateDownloadUrlWindows = "https://autostack.dk/downloads/autostack-cli-win.zip";
    private const string UpdateDownloadUrlLinux = "https://autostack.dk/downloads/autostack-cli-linux.zip";

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestVersionAsync();

            if (latestVersion != null && new Version(latestVersion) > new Version(currentVersion))
            {
                Console.WriteLine($"New version available: {latestVersion} (current: {currentVersion})");
                Console.Write("Do you want to update? (y/n): ");
                var response = Console.ReadLine()?.ToLower();

                if (response == "y" || response == "yes")
                {
                    await DownloadAndInstallUpdateAsync(latestVersion);
                    return true;
                }
            }
            else
            {
                Console.WriteLine("You're running the latest version.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update check failed: {ex.Message}");
        }

        return false;
    }

    private string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return $"{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private async Task<string?> GetLatestVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(UpdateCheckUrl);
            // Expecting JSON like: {"version": "1.0.1"}
            var versionData = System.Text.Json.JsonSerializer.Deserialize<VersionResponse>(response);
            return versionData?.Version;
        }
        catch
        {
            return null;
        }
    }

    private async Task DownloadAndInstallUpdateAsync(string version)
    {
        try
        {
            Console.WriteLine("Downloading update...");
            var downloadUrl = OperatingSystem.IsWindows() ? UpdateDownloadUrlWindows : UpdateDownloadUrlLinux;
            var zipBytes = await _httpClient.GetByteArrayAsync(downloadUrl);

            var tempPath = Path.Combine(Path.GetTempPath(), "autostack-update.zip");
            await File.WriteAllBytesAsync(tempPath, zipBytes);

            Console.WriteLine("Installing update...");
            var installPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            // Extract to temp location first
            var extractPath = Path.Combine(Path.GetTempPath(), "autostack-update");
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            ZipFile.ExtractToDirectory(tempPath, extractPath);

            // Create update script
            var scriptPath = CreateUpdateScript(extractPath, installPath);

            Console.WriteLine("Restarting to apply update...");

            // Start the update script and exit
            var startInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c \"{scriptPath}\"" : scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
        }
    }

    private string CreateUpdateScript(string sourcePath, string targetPath)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), OperatingSystem.IsWindows() ? "update.bat" : "update.sh");

        if (OperatingSystem.IsWindows())
        {
            var script = $@"
@echo off
timeout /t 2 /nobreak > nul
xcopy /Y /E ""{sourcePath}\*"" ""{targetPath}""
start """" ""{Path.Combine(targetPath, "AutoStack-CLI.exe")}""
del ""%~f0""
";
            File.WriteAllText(scriptPath, script);
        }
        else
        {
            var script = $@"#!/bin/bash
sleep 2
cp -rf {sourcePath}/* {targetPath}/
chmod +x {targetPath}/AutoStack-CLI
{targetPath}/AutoStack-CLI &
rm $0
";
            File.WriteAllText(scriptPath, script);
            Process.Start("chmod", $"+x {scriptPath}").WaitForExit();
        }

        return scriptPath;
    }

    private class VersionResponse
    {
        public string Version { get; set; } = string.Empty;
    }
}
