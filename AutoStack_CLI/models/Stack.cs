namespace AutoStack_CLI.models;

public class Stack
{
    /// <summary>
    /// Gets or sets the id of the stack
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the stack
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the stack
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the stack
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the number of times the stack has been downloaded
    /// </summary>
    public int Downloads { get; set; }

    /// <summary>
    /// Gets or sets the list of packages included in the stack
    /// </summary>
    public List<Packages>? Packages { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // USER INFO
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;
    
    public string? UserAvatarUrl { get; set; }
}