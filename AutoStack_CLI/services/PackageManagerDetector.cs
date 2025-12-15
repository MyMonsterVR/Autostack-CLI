using System.Diagnostics;

namespace AutoStack_CLI.services;

/// <summary>
/// Detects which package managers are installed on the system
/// </summary>
public class PackageManagerDetector
{
    /// <summary>
    /// Checks if a specific package manager is installed
    /// </summary>
    public static async Task<bool> IsInstalledAsync(PackageManager packageManager)
    {
        if (packageManager == PackageManager.NOTCHOSEN)
            return false;

        var command = GetCommandName(packageManager);
        return await IsCommandAvailableAsync(command);
    }

    /// <summary>
    /// Gets all installed package managers
    /// </summary>
    public static async Task<List<PackageManager>> GetInstalledAsync()
    {
        var installed = new List<PackageManager>();

        foreach (var pm in Enum.GetValues<PackageManager>())
        {
            if (pm == PackageManager.NOTCHOSEN)
                continue;

            if (await IsInstalledAsync(pm))
            {
                installed.Add(pm);
            }
        }

        return installed;
    }

    /// <summary>
    /// Gets version of a package manager (if installed)
    /// </summary>
    public static async Task<string?> GetVersionAsync(PackageManager packageManager)
    {
        if (!await IsInstalledAsync(packageManager))
            return null;

        var command = GetCommandName(packageManager);
        var versionArg = GetVersionArgument(packageManager);

        if (versionArg == null)
        {
            throw new ArgumentException($"Unknown package manager: {packageManager}");
        }

        try
        {
            var startInfo = new ProcessStartInfo();

            if (IsRunningInFlatpak())
            {
                startInfo.FileName = "flatpak-spawn";
                startInfo.Arguments = $"--host {command} {versionArg}";
            }
            else
            {
                startInfo.FileName = command;
                startInfo.Arguments = versionArg;
            }

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            // Check if running in Flatpak
            if (IsRunningInFlatpak())
            {
                // Use flatpak-spawn to check on host system
                var startInfo = new ProcessStartInfo
                {
                    FileName = "flatpak-spawn",
                    Arguments = $"--host which {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            else
            {
                // On Windows, use "where" command; on Unix, use "which"
                var isWindows = OperatingSystem.IsWindows();
                var findCommand = isWindows ? "where" : "which";

                var startInfo = new ProcessStartInfo
                {
                    FileName = findCommand,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        catch
        {
            // Command not found or other error
            return false;
        }
    }

    private static bool IsRunningInFlatpak()
    {
        // Check if running inside a Flatpak sandbox
        return File.Exists("/.flatpak-info");
    }

    private static string GetCommandName(PackageManager packageManager)
    {
        return packageManager switch
        {
            PackageManager.NPM => "npm",
            PackageManager.PNPM => "pnpm",
            PackageManager.YARN => "yarn",
            PackageManager.BUN => "bun",
            _ => throw new ArgumentException($"Unknown package manager: {packageManager}")
        };
    }

    private static string? GetVersionArgument(PackageManager packageManager)
    {
        if (packageManager == PackageManager.NOTCHOSEN)
        {
            return null;
        }
        // Most package managers use --version, but can be customized per PM
        switch (packageManager)
        {
            case PackageManager.NPM:
            case PackageManager.PNPM:
            case PackageManager.YARN:
            case PackageManager.BUN:
            default:
                return "--version";
        }
    }
}
