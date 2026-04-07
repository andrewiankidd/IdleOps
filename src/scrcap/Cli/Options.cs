namespace scrcap.Cli;

internal sealed record Options(
    string? Window,
    string Output,
    bool ShowHelp,
    bool ShowVersion);
