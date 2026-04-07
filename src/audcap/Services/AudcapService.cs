using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Capture;
using audcap.Audio;

namespace audcap.Services;

public static class AudcapService
{
    public static async Task<CaptureResult> CaptureAsync(string outputPath, double? delaySeconds = null, double? timerSeconds = null, CancellationToken token = default)
    {
        var host = HostInfo.Detect();
        var osDescription = HostInfo.Describe(host);

        var capturer = AudioCapturerFactory.Create(osDescription);
        if (capturer is null)
        {
            ConsoleLogger.Error($"No audio capturer available for {osDescription}.");
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
            var exit = await capturer.CaptureAsync(outputPath, cts.Token);
            return new CaptureResult(outputPath, startTime, exit);
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Error("Audio capture cancelled.");
            return new CaptureResult(outputPath, startTime, 1);
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Audio capture failed: {ex.Message}");
            return new CaptureResult(outputPath, startTime, 1);
        }
    }
}
