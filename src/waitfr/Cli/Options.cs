namespace waitfr.Cli;

internal record Options
{
    public string? Window { get; init; }
    public string? Text { get; init; }
    public double Timeout { get; init; } = 10;
    public bool Gone { get; init; }
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
}
