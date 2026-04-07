namespace outcap.Cli;

public record Options(
    string MergedOutput,
    bool ShowHelp,
    bool ShowVersion,
    bool KeepRaw,
    double? TimerSeconds,
    double? DelaySeconds,
    VideoOptions Video,
    AudioOptions Audio);

public record VideoOptions(string OutputPath, string? WindowTitle, double? DelaySeconds, double? TimerSeconds);

public record AudioOptions(string OutputPath, double? DelaySeconds, double? TimerSeconds);
