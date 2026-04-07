namespace vidcap.Cli;

internal record Options(string OutputPath, bool ShowHelp, bool ShowVersion, double? DelaySeconds, double? TimerSeconds, string? WindowTitle);
