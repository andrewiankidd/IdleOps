namespace spkbak.Cli;

internal record Options
{
    public string? Text { get; init; }
    public string? File { get; init; }
    public string? Output { get; init; }
    public string? Voice { get; init; }
    public bool List { get; init; }
    public bool ShowHelp { get; init; }
}
