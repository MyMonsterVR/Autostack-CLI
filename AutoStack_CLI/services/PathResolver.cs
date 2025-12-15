using System.Text.RegularExpressions;

namespace AutoStack_CLI.services;

/// <summary>
/// Resolves path placeholders like {Desktop}, {Documents}, and custom user-defined paths
/// </summary>
public partial class PathResolver
{
    private readonly ConfigurationService _configService;
    private readonly Dictionary<string, string> _builtInPaths;

    public PathResolver(ConfigurationService configService)
    {
        _configService = configService;

        // On Linux, use XDG directories; on Windows, use standard folders
        if (OperatingSystem.IsLinux())
        {
            var xdgDirs = GetXdgUserDirectories();
            _builtInPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Desktop", xdgDirs.TryGetValue("DESKTOP", out var desktop) ? desktop : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop") },
                { "Documents", xdgDirs.TryGetValue("DOCUMENTS", out var docs) ? docs : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents") },
                { "Downloads", xdgDirs.TryGetValue("DOWNLOAD", out var downloads) ? downloads : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") },
                { "Pictures", xdgDirs.TryGetValue("PICTURES", out var pics) ? pics : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures") },
                { "Music", xdgDirs.TryGetValue("MUSIC", out var music) ? music : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music") },
                { "Videos", xdgDirs.TryGetValue("VIDEOS", out var videos) ? videos : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos") },
                { "Home", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { "AppData", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) },
                { "Temp", Path.GetTempPath() }
            };
        }
        else
        {
            _builtInPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                { "Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                { "Downloads", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") },
                { "Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                { "Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                { "Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                { "Home", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { "AppData", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) },
                { "Temp", Path.GetTempPath() }
            };
        }
    }

    /// <summary>
    /// Resolves a path containing placeholders like {Desktop}/myFolder
    /// </summary>
    public async Task<string> ResolvePathAsync(string path)
    {
        var (resolvedPath, _) = await ResolvePathWithStatusAsync(path);
        return resolvedPath;
    }

    /// <summary>
    /// Resolves a path and returns whether all placeholders were successfully resolved
    /// </summary>
    public async Task<(string resolvedPath, bool allResolved)> ResolvePathWithStatusAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (path, true);

        var config = await _configService.LoadConfigAsync();
        var customPaths = config.CustomPaths ?? new Dictionary<string, string>();

        // Find all placeholders in the format {PlaceholderName}
        var matches = PlaceholderRegex().Matches(path);
        var resolvedPath = path;
        var allResolved = true;

        foreach (Match match in matches)
        {
            var placeholder = match.Groups[1].Value;
            string? resolvedValue = null;

            // Check custom paths first (they take precedence)
            if (customPaths.TryGetValue(placeholder, out var customPath))
            {
                resolvedValue = customPath;
            }
            // Then check built-in paths
            else if (_builtInPaths.TryGetValue(placeholder, out var builtInPath))
            {
                resolvedValue = builtInPath;
            }

            if (resolvedValue != null)
            {
                resolvedPath = resolvedPath.Replace(match.Value, resolvedValue);
            }
            else
            {
                Console.WriteLine($"Error: Unknown placeholder '{{{placeholder}}}'");
                allResolved = false;
            }
        }

        // Normalize path separators for the current platform
        resolvedPath = resolvedPath.Replace('/', Path.DirectorySeparatorChar)
                                   .Replace('\\', Path.DirectorySeparatorChar);

        return (resolvedPath, allResolved);
    }

    /// <summary>
    /// Adds or updates a custom path
    /// </summary>
    public async Task AddCustomPathAsync(string name, string path)
    {
        var config = await _configService.LoadConfigAsync();
        config.CustomPaths ??= new Dictionary<string, string>();

        var resolvedPath = await ResolvePathAsync(path);
        config.CustomPaths[name] = resolvedPath;

        await _configService.SavePreferencesAsync(config);
        Console.WriteLine($"Custom path '{name}' set to: {resolvedPath}");
    }

    /// <summary>
    /// Removes a custom path
    /// </summary>
    public async Task RemoveCustomPathAsync(string name)
    {
        var config = await _configService.LoadConfigAsync();

        if (config.CustomPaths?.Remove(name) == true)
        {
            await _configService.SavePreferencesAsync(config);
            Console.WriteLine($"Custom path '{name}' removed");
            return;
        }

        Console.WriteLine($"Custom path '{name}' not found");
    }

    /// <summary>
    /// Lists all available paths (built-in and custom)
    /// </summary>
    public async Task ListPathsAsync()
    {
        Console.WriteLine("\nBuilt-in paths:");
        foreach (var (name, path) in _builtInPaths.OrderBy(p => p.Key))
        {
            Console.WriteLine($"  {{{name}}} -> {path}");
        }

        var config = await _configService.LoadConfigAsync();
        if (config.CustomPaths?.Any() == true)
        {
            Console.WriteLine("\nCustom paths:");
            foreach (var (name, path) in config.CustomPaths.OrderBy(p => p.Key))
            {
                Console.WriteLine($"  {{{name}}} -> {path}");
            }
        }
        else
        {
            Console.WriteLine("\nNo custom paths defined.");
        }
    }

    /// <summary>
    /// Reads XDG user directories from ~/.config/user-dirs.dirs (Linux)
    /// </summary>
    private static Dictionary<string, string> GetXdgUserDirectories()
    {
        var dirs = new Dictionary<string, string>();
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configFile = Path.Combine(homeDir, ".config", "user-dirs.dirs");

        if (!File.Exists(configFile))
            return dirs;

        try
        {
            var lines = File.ReadAllLines(configFile);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    continue;

                // Parse lines like: XDG_DESKTOP_DIR="$HOME/Desktop"
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Replace("XDG_", "").Replace("_DIR", "");
                var value = parts[1].Trim('"', '\'');

                // Replace $HOME with actual home directory
                value = value.Replace("$HOME", homeDir);

                dirs[key] = value;
            }
        }
        catch
        {
            // If we can't read the file, just return empty dictionary
        }

        return dirs;
    }

    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.None, "en-gb")]
    private static partial Regex PlaceholderRegex();
}
