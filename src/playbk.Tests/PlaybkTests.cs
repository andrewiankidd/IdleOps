using System.Diagnostics;
using Xunit;

namespace playbk.Tests;

public class PlaybkTests
{
    [Fact]
    public async Task ProducesVideoFromScript()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Capture uses vidcap's Windows path in tests.
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
