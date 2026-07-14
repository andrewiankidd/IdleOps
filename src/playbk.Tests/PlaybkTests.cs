using System.Diagnostics;
using Xunit;

namespace playbk.Tests;

public class PlaybkTests
{
    [Fact]
    public async Task SpeakActionWithoutTextFailsCleanly()
    {
        // Proves the dispatcher has a `speak` case (otherwise we'd see
        // "Unknown action 'speak'") AND that the early-return guard fires
        // before any process is spawned. Hardware-free.
        var tempRoot = Path.Combine(Path.GetTempPath(), "playbk-tests-speak-guard");
        Directory.CreateDirectory(tempRoot);
        var outputDir = Path.Combine(tempRoot, "outputs");
        Directory.CreateDirectory(outputDir);

        var scriptPath = Path.Combine(tempRoot, "speak-no-text.idleops.yaml");
        File.WriteAllText(scriptPath, """
            steps:
              - name: Speak with nothing
                action: speak
            """);

        var runner = new playbk.Execution.ScriptRunner(tempRoot, outputDir);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var rc = await runner.RunAsync(scriptPath, cts.Token);

        Assert.Equal(1, rc);
    }

    [Fact]
    public async Task UnknownActionStillFails()
    {
        // Regression guard: adding `speak` shouldn't have weakened the default
        // unknown-action handling.
        var tempRoot = Path.Combine(Path.GetTempPath(), "playbk-tests-unknown");
        Directory.CreateDirectory(tempRoot);
        var outputDir = Path.Combine(tempRoot, "outputs");
        Directory.CreateDirectory(outputDir);

        var scriptPath = Path.Combine(tempRoot, "unknown.idleops.yaml");
        File.WriteAllText(scriptPath, """
            steps:
              - name: Bogus
                action: not-a-real-action
            """);

        var runner = new playbk.Execution.ScriptRunner(tempRoot, outputDir);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var rc = await runner.RunAsync(scriptPath, cts.Token);

        Assert.Equal(1, rc);
    }

    [Fact]
    public async Task ProducesVideoFromScript()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Capture uses vidcap's Windows path in tests.
            return;
        }

        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            // Skip in CI — requires display, ffmpeg, and a launchable browser.
            return;
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "playbk-tests");
        Directory.CreateDirectory(tempRoot);
        var outputDir = Path.Combine(tempRoot, "outputs");
        Directory.CreateDirectory(outputDir);

        var scriptPath = Path.Combine(tempRoot, "test.idleops.yaml");
        File.WriteAllText(scriptPath, """
audcap: false
vidcap: true
steps:
  - name: open example
    action: exec
    args: https://example.com
""");

        var runner = new playbk.Execution.ScriptRunner(tempRoot, outputDir, captureTimerSeconds: 5);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await runner.RunAsync(scriptPath, cts.Token);

        var video = Directory.EnumerateFiles(outputDir, "*-video.mp4", SearchOption.TopDirectoryOnly).FirstOrDefault();
        Assert.False(string.IsNullOrEmpty(video), "No video file produced.");
        Assert.True(new FileInfo(video!).Length > 10_000, "Video file is empty.");
    }
}
