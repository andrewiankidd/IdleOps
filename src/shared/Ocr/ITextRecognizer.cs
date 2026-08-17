namespace IdleOps.Shared.Ocr;

/// <summary>
/// Recognizes words (with bounding boxes) in an image file. Windows uses WinRT OCR
/// (in shared.win); Linux/macOS use Tesseract. Selected per-platform by the caller
/// (txtfnd) since the WinRT implementation only exists on the Windows TFM.
/// </summary>
public interface ITextRecognizer
{
    /// <summary>Human label for logs, e.g. "winrt-ocr" or "tesseract".</summary>
    string Name { get; }

    /// <summary>Recognize all words in the image at <paramref name="imagePath"/>.</summary>
    Task<IReadOnlyList<RecognizedWord>> RecognizeAsync(string imagePath);
}
