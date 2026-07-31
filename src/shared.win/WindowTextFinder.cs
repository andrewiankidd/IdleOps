using System.Runtime.Versioning;
using Windows.Media.Ocr;
using IdleOps.Shared.Windows;

namespace IdleOps.Shared.Win;

/// <summary>
/// Finds on-screen text within a window via OCR and returns its click coordinates
/// (window-relative). Holds a single warm <see cref="OcrEngine"/> reused across
/// calls, so a runbook with many click-text/assert-text steps pays engine
/// initialization once instead of once per step. Windows-only.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed class WindowTextFinder
{
    private OcrEngine? _engine;

    public WindowTextFinder()
    {
        IdleOps.Shared.Platform.PlatformSupport.RequireWindows("OCR");
    }

    private OcrEngine Engine =>
        _engine ??= OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException("No OCR language available. Install a language pack in Windows Settings.");

    /// <summary>The title of the window matched by the pattern, or null if none.</summary>
    public string? ResolveTitle(string windowPattern)
        => WindowMatcher.FindWindow(windowPattern, preferNewest: true)?.Title;

    /// <summary>
    /// Locate <paramref name="text"/> in the window matching <paramref name="windowPattern"/>.
    /// Returns window-relative (x, y) of the match center, or null if the window or
    /// text is not found. Throws only on capture/OCR engine failure.
    /// </summary>
    public async Task<(int x, int y)?> FindAsync(string windowPattern, string text)
    {
        var match = WindowMatcher.FindWindow(windowPattern, preferNewest: true);
        if (match is null) return null;

        using var bitmap = WindowCapture.CaptureWindow(match.Handle);
        return await OcrService.FindTextAsync(Engine, bitmap, text);
    }

    /// <summary>All text recognized in the matched window (for diagnostics).</summary>
    public async Task<IReadOnlyList<OcrTextResult>> RecognizeAllAsync(string windowPattern)
    {
        var match = WindowMatcher.FindWindow(windowPattern, preferNewest: true);
        if (match is null) return [];

        using var bitmap = WindowCapture.CaptureWindow(match.Handle);
        return await OcrService.RecognizeAllAsync(Engine, bitmap);
    }
}
