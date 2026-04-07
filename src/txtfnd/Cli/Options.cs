namespace txtfnd.Cli;

internal sealed record Options(
    string? Window,
    string? Text,
    bool ShowHelp,
    bool ShowVersion);
