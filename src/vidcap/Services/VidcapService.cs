using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Capture;
using vidcap.Video;

namespace vidcap.Services;

public static class VidcapService
{
    public static async Task<CaptureResult> CaptureAsync(string outputPath, string? windowTitle = null, double? delaySeconds = null, double? timerSeconds = null, CancellationToken token = default)
    {
        var host = HostInfo.Detect();
        var osDescription = HostInfo.Describe(host);

        var capturer = VideoCapturerFactory.Create(osDescription);
        if (capturer is null)
        {
            ConsoleLogger.Error($"No video capturer available for {osDescription}.");
            return new CaptureResult(outputPath, DateTimeOffset.UtcNow, 1);
        }

        if (delaySeconds is { } delay && delay > 0)
        {
            ConsoleLogger.Info($"Delaying start by {delay:0.##}s...");
            await Task.Delay(TimeSpan.FromSeconds(delay), token);
        }

        var startTime = DateTimeOffset.UtcNow;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (timerSeconds is { } timer && timer > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timer));
        }

        try
        {
            var exit = await capturer.CaptureAsync(outputPath, windowTitle, cts.Token);
            return new CaptureResult(outputPath, startTime, exit);
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Error("Video capture cancelled.");
            return new CaptureResult(outputPath, startTime, 1);
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Video capture failed: {ex.Message}");
            return new CaptureResult(outputPath, startTime, 1);
        }
    }
}
