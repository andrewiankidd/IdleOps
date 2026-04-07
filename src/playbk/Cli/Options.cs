namespace playbk.Cli;

internal record Options(IReadOnlyList<string> InputPatterns, string OutputDirectory, bool ShowHelp, bool ShowVersion);
