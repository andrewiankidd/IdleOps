namespace IdleOps.Shared.Capture;

public sealed record CaptureResult(string OutputPath, DateTimeOffset StartTimeUtc, int ExitCode);
