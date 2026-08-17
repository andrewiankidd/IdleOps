namespace playbk.Cli;

internal record Options(
    IReadOnlyList<string> InputPatterns,
    string OutputDirectory,
    bool ShowHelp,
    bool ShowVersion,
    string? Goal = null,
    bool DryRun = false,
    string Profile = "local");
