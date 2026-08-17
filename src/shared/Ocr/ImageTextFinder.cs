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

    /// <summary>
    /// All words recognized in the matched window (empty if the window wasn't found),
    /// with boxes in the window's own coordinate space — so a caller can click what it
    /// finds without knowing the display's pixel density.
    /// </summary>
    public async Task<IReadOnlyList<RecognizedWord>> RecognizeAllAsync(string windowPattern)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"txtfnd-{Guid.NewGuid():N}.png");
        try
        {
            var outcome = _capturer.Capture(windowPattern, tmp);
            if (!outcome.Ok) return [];              // capturer already logged why (e.g. window not found)

            var words = await _recognizer.RecognizeAsync(tmp);
            // Recognition runs on the full-resolution image (more pixels, better OCR) and
            // the results are converted afterwards, so Retina detail is not thrown away.
            return ToWindowSpace(words, outcome.Scale);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best-effort */ }
        }
    }

    // On a 2x display the capture is twice the window's coordinate size, so every box has
    // to be halved before it means anything to a click.
    private static IReadOnlyList<RecognizedWord> ToWindowSpace(IReadOnlyList<RecognizedWord> words, double scale)
    {
        if (scale is <= 0 or 1.0 || words.Count == 0) return words;

        var scaled = new List<RecognizedWord>(words.Count);
        foreach (var w in words)
        {
            scaled.Add(w with
            {
                X = (int)Math.Round(w.X / scale),
                Y = (int)Math.Round(w.Y / scale),
                Width = (int)Math.Round(w.Width / scale),
                Height = (int)Math.Round(w.Height / scale),
            });
        }
        return scaled;
    }

    /// <summary>Window-relative (x, y) of the text's match center, or null if not found.</summary>
    public async Task<(int x, int y)?> FindAsync(string windowPattern, string text)
        => TextLocator.Locate(await RecognizeAllAsync(windowPattern), text);
}
