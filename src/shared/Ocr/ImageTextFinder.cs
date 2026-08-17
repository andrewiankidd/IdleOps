using IdleOps.Shared.Capture;

namespace IdleOps.Shared.Ocr;

/// <summary>
/// Finds on-screen text in a window: captures the window with an
/// <see cref="IScreenCapturer"/>, recognizes words with an <see cref="ITextRecognizer"/>,
/// and locates the target via <see cref="TextLocator"/>. The cross-platform
/// equivalent of shared.win's WindowTextFinder — capture and recognizer are injected
/// so the same flow serves Windows (WinRT) and Linux/macOS (Tesseract).
/// </summary>
public sealed class ImageTextFinder
{
    private readonly IScreenCapturer _capturer;
    private readonly ITextRecognizer _recognizer;

    public ImageTextFinder(IScreenCapturer capturer, ITextRecognizer recognizer)
    {
        _capturer = capturer;
        _recognizer = recognizer;
    }

    public string RecognizerName => _recognizer.Name;

    /// <summary>All words recognized in the matched window (empty if the window wasn't found).</summary>
    public async Task<IReadOnlyList<RecognizedWord>> RecognizeAllAsync(string windowPattern)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"txtfnd-{Guid.NewGuid():N}.png");
        try
        {
            var outcome = _capturer.Capture(windowPattern, tmp);
            if (!outcome.Ok) return [];              // capturer already logged why (e.g. window not found)
            return await _recognizer.RecognizeAsync(tmp);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best-effort */ }
        }
    }

    /// <summary>Window-relative (x, y) of the text's match center, or null if not found.</summary>
    public async Task<(int x, int y)?> FindAsync(string windowPattern, string text)
        => TextLocator.Locate(await RecognizeAllAsync(windowPattern), text);
}
