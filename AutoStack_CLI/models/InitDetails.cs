namespace AutoStack_CLI.models;

public struct InitDetails
{
    public bool DetailsEnabled { get; set; }
    public string ProjectName { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string EntryPoint { get; set; }
}
