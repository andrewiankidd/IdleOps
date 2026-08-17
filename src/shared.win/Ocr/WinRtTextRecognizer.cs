using System.Drawing;
using System.Runtime.Versioning;
using Windows.Media.Ocr;
using IdleOps.Shared.Ocr;

namespace IdleOps.Shared.Win;

/// <summary>
/// Windows OCR via WinRT (<see cref="OcrEngine"/>), implementing the cross-platform
/// <see cref="ITextRecognizer"/>. Holds a warm engine and reuses the existing
/// <see cref="OcrService"/> (with its upscale-for-small-UI-text accuracy trick).
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed class WinRtTextRecognizer : ITextRecognizer
{
    private OcrEngine? _engine;

    public WinRtTextRecognizer()
    {
        IdleOps.Shared.Platform.PlatformSupport.RequireWindows("OCR");
    }

    public string Name => "winrt-ocr";

    private OcrEngine Engine =>
        _engine ??= OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException("No OCR language available. Install a language pack in Windows Settings.");

    public async Task<IReadOnlyList<RecognizedWord>> RecognizeAsync(string imagePath)
    {
        using var bitmap = new Bitmap(imagePath);
        var results = await OcrService.RecognizeAllAsync(Engine, bitmap);
        return results
            .Select(r => new RecognizedWord(r.Text, r.X, r.Y, r.Width, r.Height))
            .ToList();
    }
}
