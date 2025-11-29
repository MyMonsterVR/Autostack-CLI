using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoStack_CLI.services;

/// <summary>
/// Manages secure storage of user configuration including credentials and preferences
/// </summary>
public class ConfigurationService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly string _credentialsFilePath;

    public ConfigurationService()
    {
        // Get cross-platform config directory
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoStack"
        );

        _configFilePath = Path.Combine(_configDirectory, "config.json");
        _credentialsFilePath = Path.Combine(_configDirectory, ".credentials");

        // Ensure directory exists
        Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    /// Saves user credentials securely
    /// </summary>
    public async Task SaveCredentialsAsync(string username, string authToken)
    {
        var credentials = new
        {
            Username = username,
            AuthToken = authToken,
            LastLogin = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(credentials);
        var encryptedData = EncryptString(json);

        await File.WriteAllBytesAsync(_credentialsFilePath, encryptedData);

        // Set restrictive file permissions on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(_credentialsFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Loads saved credentials
    /// </summary>
    public async Task<(string? Username, string? AuthToken, DateTime? LastLogin)> LoadCredentialsAsync()
    {
        if (!File.Exists(_credentialsFilePath))
        {
            return (null, null, null);
        }

        try
        {
            var encryptedData = await File.ReadAllBytesAsync(_credentialsFilePath);
            var json = DecryptString(encryptedData);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var username = root.GetProperty("Username").GetString();
            var authToken = root.GetProperty("AuthToken").GetString();
            var lastLogin = root.GetProperty("LastLogin").GetDateTime();

            return (username, authToken, lastLogin);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load credentials: {ex.Message}");
            return (null, null, null);
        }
    }

    /// <summary>
    /// Saves user preferences (non-sensitive data)
    /// </summary>
    public async Task SavePreferencesAsync(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_configFilePath, json);
    }

    /// <summary>
    /// Loads user preferences
    /// </summary>
    public async Task<Config> LoadPreferencesAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            return new Config();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load preferences: {ex.Message}");
            return new Config();
        }
    }

    /// <summary>
    /// Saves both credentials and preferences
    /// </summary>
    public async Task SaveConfigAsync(string username, string authToken, PackageManager packageManager = PackageManager.NOTCHOSEN)
    {
        // Save credentials securely (username, token, timestamp)
        await SaveCredentialsAsync(username, authToken);

        // Save preferences (non-sensitive - just package manager choice)
        var config = new Config
        {
            ChosenPackageManager = packageManager
        };

        await SavePreferencesAsync(config);
    }

    /// <summary>
    /// Loads preferences (just package manager choice)
    /// Note: Does NOT return AuthToken/Username/LastLogin - use LoadCredentialsAsync() for those
    /// </summary>
    public async Task<Config> LoadConfigAsync()
    {
        return await LoadPreferencesAsync();
    }

    /// <summary>
    /// Gets just the auth token from secure storage
    /// </summary>
    public async Task<string?> GetAuthTokenAsync()
    {
        var (_, authToken, _) = await LoadCredentialsAsync();
        return authToken;
    }

    /// <summary>
    /// Clears all saved credentials and preferences
    /// </summary>
    public void ClearConfig()
    {
        if (File.Exists(_credentialsFilePath))
        {
            File.Delete(_credentialsFilePath);
        }

        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
    }

    /// <summary>
    /// Checks if user is logged in
    /// </summary>
    public async Task<bool> IsLoggedInAsync()
    {
        var (_, authToken, _) = await LoadCredentialsAsync();
        return !string.IsNullOrEmpty(authToken);
    }

    /// <summary>
    /// Checks if config file exists (useful for first-time setup)
    /// </summary>
    public bool ConfigExists()
    {
        return File.Exists(_configFilePath);
    }

    /// <summary>
    /// Checks if credentials file exists (useful for checking if user has ever logged in)
    /// </summary>
    public bool CredentialsExist()
    {
        return File.Exists(_credentialsFilePath);
    }

    /// <summary>
    /// Encrypts a string using platform-appropriate methods
    /// </summary>
    private static byte[] EncryptString(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Use Windows DPAPI for strong encryption
        // For Unix-like systems, use AES with a machine-specific key
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser)
            : EncryptWithAes(plainBytes);
    }

    /// <summary>
    /// Decrypts data using platform-appropriate methods
    /// </summary>
    private static string DecryptString(byte[] encryptedData)
    {
        byte[] plainBytes;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use Windows DPAPI
            plainBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            // For Unix-like systems, use AES
            plainBytes = DecryptWithAes(encryptedData);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// AES encryption for non-Windows platforms
    /// </summary>
    private static byte[] EncryptWithAes(byte[] plainBytes)
    {
        using var aes = Aes.Create();
        aes.Key = GetMachineKey();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return result;
    }

    /// <summary>
    /// AES decryption for non-Windows platforms
    /// </summary>
    private static byte[] DecryptWithAes(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = GetMachineKey();

        // Extract IV from the beginning
        var iv = new byte[aes.IV.Length];
        var encrypted = new byte[encryptedData.Length - iv.Length];

        Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, iv.Length, encrypted, 0, encrypted.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }

    /// <summary>
    /// Generates a machine-specific encryption key
    /// </summary>
    private static byte[] GetMachineKey()
    {
        // Use machine name + user name as basis for key
        // This provides basic protection - for production use, consider more robust key management
        var keySource = $"{Environment.MachineName}_{Environment.UserName}_AutoStack_v1";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
    }
}
