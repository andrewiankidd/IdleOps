namespace imgfnd.Cli;

internal record Options
{
    public string? Window { get; init; }
    public string? ImagePath { get; init; }
    public double Threshold { get; init; } = 0.8;
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
}
