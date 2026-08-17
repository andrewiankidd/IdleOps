using System.Globalization;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;

namespace IdleOps.Shared.Capture;

/// <summary>
/// macOS capturer via the built-in `screencapture`. Whole-display capture uses
/// `-x`; per-window capture reads the window's bounds from <see cref="Windowing.MacWindowLocator"/>
/// (osascript) and captures that region with `-R`.
///
/// UNVERIFIED: written without a Mac to test on. Per-window region capture needs
/// Accessibility permission (for the osascript bounds lookup).
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacScreenCapturer : IScreenCapturer
{
    public string Name => "screencapture";

    public CaptureOutcome Capture(string windowPattern, string outputPath)
    {
        string[] args;
        if (ScreenCapturerFactory.IsWholeScreen(windowPattern))
        {
            Console.Error.WriteLine($"[scrcap] Capturing screen -> {outputPath}");
            args = ["-x", outputPath];
        }
        else
        {
            var bounds = new Windowing.MacWindowLocator().GetBounds(windowPattern);
            if (bounds is not { } b)
            {
                ConsoleLogger.Error($"Window '{windowPattern}' not found.");
                return CaptureOutcome.Failed;
            }
            Console.Error.WriteLine($"[scrcap] Capturing window region {b.Width}x{b.Height} at {b.X},{b.Y} -> {outputPath}");
            args = ["-x", $"-R{b.X},{b.Y},{b.Width},{b.Height}", outputPath];
        }

        var (ok, _, stderr) = ProcessRunner.Run("screencapture", args);
        if (!ok)
        {
            ConsoleLogger.Error($"screencapture failed: {stderr.Trim()}");
            return CaptureOutcome.Failed;
        }

        var (w, h) = Dimensions(outputPath);
        return new CaptureOutcome(true, w, h);
    }

    // Read pixel size back with `sips` (built-in); 0x0 if it can't be parsed.
    private static (int w, int h) Dimensions(string path)
    {
        var (ok, stdout, _) = ProcessRunner.Run("sips", "-g", "pixelWidth", "-g", "pixelHeight", path);
        if (!ok) return (0, 0);

        int w = 0, h = 0;
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0 || !int.TryParse(line[(idx + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) continue;
            if (line.Contains("pixelWidth")) w = v;
            else if (line.Contains("pixelHeight")) h = v;
        }
        return (w, h);
    }
}
