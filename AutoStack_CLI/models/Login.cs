namespace AutoStack_CLI.models;

/// <summary>
/// Response DTO containing authentication tokens for a successful login
/// </summary>
public class Login
{
    /// <summary>
    /// Gets or sets the JWT access token for API authentication
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens
    /// </summary>
    public required string RefreshToken { get; set; }
}