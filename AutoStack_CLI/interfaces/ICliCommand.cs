namespace AutoStack_CLI.interfaces;

public interface ICliCommand
{
    /// <summary>
    /// The command name (e.g., "install", "login")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description shown in help text
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Usage example (e.g., "install <id>")
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Execute command from CLI string arguments
    /// </summary>
    Task ExecuteAsync(string[] args);
}
