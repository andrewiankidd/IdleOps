using System.Globalization;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;

namespace IdleOps.Shared.Capture;

/// <summary>
/// Linux (X11) capturer. Resolves the window id via xdotool, then saves it with
/// ImageMagick `import -window &lt;id&gt; &lt;path&gt;` (format inferred from the extension).
/// "root"/"screen"/"desktop" captures the whole display.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxScreenCapturer : IScreenCapturer
{
    public string Name => "imagemagick (X11)";

    public CaptureOutcome Capture(string windowPattern, string outputPath)
    {
        if (!ProcessRunner.ToolExists("import"))
        {
            ConsoleLogger.Error("scrcap on Linux needs ImageMagick (`import`) on PATH.");
            return CaptureOutcome.Failed;
        }

        var wholeScreen = ScreenCapturerFactory.IsWholeScreen(windowPattern);
        var id = wholeScreen ? "root" : Windowing.LinuxX11Windows.SearchId(windowPattern);
        if (id is null)
        {
            ConsoleLogger.Error($"Window '{windowPattern}' not found.");
            return CaptureOutcome.Failed;
        }

        Console.Error.WriteLine($"[scrcap] Capturing {(wholeScreen ? "screen" : $"window {id}")} -> {outputPath}");
        var (ok, _, stderr) = ProcessRunner.Run("import", "-window", id, outputPath);
        if (!ok)
        {
            ConsoleLogger.Error($"import failed: {stderr.Trim()}");
            return CaptureOutcome.Failed;
        }

        var (w, h) = Geometry(id);
        return new CaptureOutcome(true, w, h);
    }

    // Best-effort pixel size via xdotool; 0x0 if unavailable (doesn't fail the capture).
    private static (int w, int h) Geometry(string id)
    {
        var (ok, stdout, _) = id == "root"
            ? ProcessRunner.Run("xdotool", "getdisplaygeometry")
            : ProcessRunner.Run("xdotool", "getwindowgeometry", "--shell", id);
        if (!ok) return (0, 0);

        if (id == "root")
        {
            var parts = stdout.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 && int.TryParse(parts[0], out var dw) && int.TryParse(parts[1], out var dh)
                ? (dw, dh) : (0, 0);
        }

        int w = 0, h = 0;
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = line.IndexOf('=');
            if (eq <= 0 || !int.TryParse(line[(eq + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) continue;
            if (line[..eq] == "WIDTH") w = v;
            else if (line[..eq] == "HEIGHT") h = v;
        }
        return (w, h);
    }
}
