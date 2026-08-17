using System.Drawing.Imaging;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Windows;

namespace IdleOps.Shared.Capture;

/// <summary>Windows capturer: GDI PrintWindow/BitBlt via WindowCapture, saved with System.Drawing.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsScreenCapturer : IScreenCapturer
{
    public string Name => "gdi";

    public CaptureOutcome Capture(string windowPattern, string outputPath)
    {
        var match = WindowMatcher.FindWindow(windowPattern, preferNewest: true);
        if (match is null)
        {
            ConsoleLogger.Error($"Window '{windowPattern}' not found.");
            return CaptureOutcome.Failed;
        }

        Console.Error.WriteLine($"[scrcap] Capturing window '{match.Title}' -> {outputPath}");
        using var bitmap = WindowCapture.CaptureWindow(match.Handle);
        bitmap.Save(outputPath, FormatFor(outputPath));
        return new CaptureOutcome(true, bitmap.Width, bitmap.Height);
    }

    private static ImageFormat FormatFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => ImageFormat.Jpeg,
        ".bmp" => ImageFormat.Bmp,
        ".gif" => ImageFormat.Gif,
        ".tiff" or ".tif" => ImageFormat.Tiff,
        _ => ImageFormat.Png,
    };
}
