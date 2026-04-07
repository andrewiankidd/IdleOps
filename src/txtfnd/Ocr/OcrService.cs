using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace txtfnd.Ocr;

[SupportedOSPlatform("windows10.0.22621.0")]
internal static class OcrService
{
    /// <summary>
    /// Find the center coordinates of a text match within a bitmap.
    /// Returns (x, y) relative to the bitmap origin, or null if not found.
    /// </summary>
    public static async Task<(int x, int y)?> FindTextAsync(Bitmap bitmap, string searchText)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            throw new InvalidOperationException("No OCR language available. Install a language pack in Windows Settings.");
        }

        using var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);

        // Try exact single-word match first, then substring across consecutive words
        foreach (var line in result.Lines)
        {
            // Check if the entire line text contains the search string
            if (line.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                // Find which word(s) contain/span the match
                var match = FindWordSpan(line.Words, searchText);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get all recognized text from a bitmap (for debugging).
    /// </summary>
    public static async Task<IReadOnlyList<OcrTextResult>> RecognizeAllAsync(Bitmap bitmap)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            return [];
        }

        using var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);

        var results = new List<OcrTextResult>();
        foreach (var line in result.Lines)
        {
            foreach (var word in line.Words)
            {
                var r = word.BoundingRect;
                results.Add(new OcrTextResult(
                    word.Text,
                    (int)r.X, (int)r.Y,
                    (int)r.Width, (int)r.Height));
            }
        }

        return results;
    }

    private static (int x, int y)? FindWordSpan(IReadOnlyList<OcrWord> words, string searchText)
    {
        // Single word match
        foreach (var word in words)
        {
            if (word.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                var r = word.BoundingRect;
                return ((int)(r.X + r.Width / 2), (int)(r.Y + r.Height / 2));
            }
        }

        // Multi-word span match (e.g., "Save As" spanning two OcrWords)
        for (var i = 0; i < words.Count; i++)
        {
            var combined = words[i].Text;
            var firstRect = words[i].BoundingRect;
            var lastRect = firstRect;

            for (var j = i; j < words.Count; j++)
            {
                if (j > i)
                {
                    combined += " " + words[j].Text;
                    lastRect = words[j].BoundingRect;
                }

                if (combined.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    // Return center of the bounding box spanning all matched words
                    var left = Math.Min(firstRect.X, lastRect.X);
                    var top = Math.Min(firstRect.Y, lastRect.Y);
                    var right = Math.Max(firstRect.X + firstRect.Width, lastRect.X + lastRect.Width);
                    var bottom = Math.Max(firstRect.Y + firstRect.Height, lastRect.Y + lastRect.Height);
                    return ((int)(left + (right - left) / 2), (int)(top + (bottom - top) / 2));
                }
            }
        }

        return null;
    }

    private static async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        var stream = new InMemoryRandomAccessStream();
        await ms.CopyToAsync(stream.AsStreamForWrite());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}

internal sealed record OcrTextResult(string Text, int X, int Y, int Width, int Height);
