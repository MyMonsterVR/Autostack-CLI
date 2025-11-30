using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace AutoStack_CLI.services;

public class UpdateService(ApiConfiguration config)
{
    private readonly HttpClient httpClient = new();

    public async Task<bool> CheckForUpdatesAsync(string[] args)
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var latestVersion = await GetLatestVersionAsync();

            if (latestVersion == null)
            {
                return false;
            }

            if (new Version(latestVersion) > new Version(currentVersion))
            {
                Console.WriteLine($"New version available: {latestVersion}");
                Console.Write("Do you want to update? (y/n): ");
                var response = Console.ReadKey(true);

                if (response.Key == ConsoleKey.Y)
                {
                    await DownloadAndInstallUpdateAsync(args);
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

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version == null) return "1.0.0";

        var major = version.Major;
        var minor = version.Minor;
        var build = version.Build >= 0 ? version.Build : 0;

        return $"{major}.{minor}.{build}";
    }

    private async Task<string?> GetLatestVersionAsync()
    {
        try
        {
            var response = await httpClient.GetStringAsync(config.UpdateCheckUrl);
            // Expecting JSON like: {"version": "1.0.1"}
            var versionData = System.Text.Json.JsonSerializer.Deserialize<VersionResponse>(response);
            return versionData?.Version;
        }
        catch
        {
            return null;
        }
    }

    private async Task DownloadAndInstallUpdateAsync(string[] args)
    {
        try
        {
            Console.WriteLine("Downloading update...");
            var downloadUrl = OperatingSystem.IsWindows() ? config.UpdateDownloadUrlWindows : config.UpdateDownloadUrlLinux;
            var zipBytes = await httpClient.GetByteArrayAsync(downloadUrl);

            var tempPath = Path.Combine(Path.GetTempPath(), "autostack-update.zip");
            await File.WriteAllBytesAsync(tempPath, zipBytes);

            Console.WriteLine("Installing update...");

            // Use ProcessPath for single-file apps
            var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var installPath = Path.GetDirectoryName(exePath);

            if (string.IsNullOrEmpty(installPath))
            {
                Console.WriteLine("Update failed: Cannot determine installation directory.");
                return;
            }

            // Extract to temp location first
            var extractPath = Path.Combine(Path.GetTempPath(), "autostack-update");
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            await ZipFile.ExtractToDirectoryAsync(tempPath, extractPath);

            var scriptPath = CreateUpdateScript(extractPath, installPath, args);

            Console.WriteLine("Update downloaded successfully!");
            Console.WriteLine("Press any key to restart and apply update...");
            Console.ReadKey();

            var startInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update failed: {ex.Message}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    private static string CreateUpdateScript(string sourcePath, string targetPath, string[] args)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), OperatingSystem.IsWindows() ? "update.bat" : "update.sh");
        var exeName = Path.GetFileName(Environment.ProcessPath ?? "AutoStack_CLI.exe");
        var exePath = Path.Combine(targetPath, exeName);

        // Build argument string for restart command
        var argsString = args.Length > 0 ? string.Join(" ", args.Select(a => $"\"{a}\"")) : "";

        if (OperatingSystem.IsWindows())
        {
            var script = $@"@echo off
                echo Waiting for application to close...
                timeout /t 2 /nobreak > nul
                echo Copying files from: {sourcePath}
                echo To: {targetPath}
                xcopy /Y /E /I ""{sourcePath}\*"" ""{targetPath}\""
                if errorlevel 1 (
                    echo ERROR: Failed to copy files! Error code: %errorlevel%
                    pause
                    exit /b 1
                )
                echo Files copied successfully!
                echo Restarting application...
                start """" ""{exePath}"" {argsString}
                del ""%~f0""
            ";
            File.WriteAllText(scriptPath, script);
        }
        else
        {
            var exeNameNoExt = Path.GetFileNameWithoutExtension(exeName);
            var linuxExePath = $"{targetPath}/{exeNameNoExt}";
            var script = $@"#!/bin/bash
                sleep 2
                cp -rf {sourcePath}/* {targetPath}/
                chmod +x {linuxExePath}
                {linuxExePath} {argsString} &
                rm $0
            ";
            File.WriteAllText(scriptPath, script);
            Process.Start("chmod", $"+x {scriptPath}").WaitForExit();
        }

        return scriptPath;
    }

    private sealed class VersionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}
