using System.Globalization;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;

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
        int requestedWidth;
        if (ScreenCapturerFactory.IsWholeScreen(windowPattern))
        {
            Console.Error.WriteLine($"[scrcap] Capturing screen -> {outputPath}");
            args = ["-x", outputPath];
            requestedWidth = 0;   // no rect to compare against; probed below instead
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
            requestedWidth = b.Width;
        }

        var (ok, _, stderr) = ProcessRunner.Run("screencapture", args);
        if (!ok)
        {
            // "could not create image from display" is what a Screen Recording denial
            // looks like — say so, rather than echoing a message about nothing.
            ConsoleLogger.Error(MacPermissions.IndicatesScreenRecordingDenied(stderr)
                ? MacPermissions.ScreenRecordingHint
                : $"screencapture failed: {stderr.Trim()}");
            return CaptureOutcome.Failed;
        }

        var (w, h) = Dimensions(outputPath);

        // `-R` is given in points but the file comes back in native pixels, so a window
        // asked for at 656x422 arrives as 1312x844 on a 2x display. Deriving the factor
        // from this capture is exact and needs no display API; the whole-screen path has
        // no such rect, so it falls back to the probe.
        var scale = requestedWidth > 0 && w > 0 ? (double)w / requestedWidth : BackingScale();
        return new CaptureOutcome(true, w, h, scale <= 0 ? 1.0 : scale);
    }

    // Backing-scale probe for whole-screen captures: grab a known-size region and see how
    // many pixels come back. Cached — it costs a capture, and it cannot change mid-run
    // without the display changing. Reflects the main display, so a mixed-DPI multi-monitor
    // setup reports the main display's factor.
    private static double? _backingScale;

    private static double BackingScale()
    {
        if (_backingScale is { } cached) return cached;

        const int probePoints = 64;
        var probe = Path.Combine(Path.GetTempPath(), $"idleops-scale-{Guid.NewGuid():N}.png");
        try
        {
            var (ok, _, _) = ProcessRunner.Run("screencapture", "-x", $"-R0,0,{probePoints},{probePoints}", probe);
            var (pw, _) = ok ? Dimensions(probe) : (0, 0);
            _backingScale = pw > 0 ? (double)pw / probePoints : 1.0;
        }
        catch
        {
            _backingScale = 1.0;
        }
        finally
        {
            try { if (File.Exists(probe)) File.Delete(probe); } catch { /* best-effort */ }
        }
        return _backingScale.Value;
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
