using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace IdleOps.Shared.Win;

/// <summary>
/// OCR search over a bitmap. The <see cref="OcrEngine"/> is passed in (not created
/// here) so callers can reuse a warm engine across many searches — creating one per
/// call is the per-step cost that made shelling out to txtfnd expensive.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
internal static class OcrService
{
    /// <summary>
    /// Find the center coordinates of a text match within a bitmap.
    /// Returns (x, y) relative to the bitmap origin, or null if not found.
    /// </summary>
    public static async Task<(int x, int y)?> FindTextAsync(OcrEngine engine, Bitmap bitmap, string searchText)
    {
        using var softwareBitmap = ToSoftwareBitmap(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);

        foreach (var line in result.Lines)
        {
            if (line.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                var match = FindWordSpan(line.Words, searchText);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    /// <summary>Get all recognized text from a bitmap (for debugging).</summary>
    public static async Task<IReadOnlyList<OcrTextResult>> RecognizeAllAsync(OcrEngine engine, Bitmap bitmap)
    {
        using var softwareBitmap = ToSoftwareBitmap(bitmap);
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
        foreach (var word in words)
        {
            if (word.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                var r = word.BoundingRect;
                return ((int)(r.X + r.Width / 2), (int)(r.Y + r.Height / 2));
            }
        }

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

    // Copy the bitmap's pixels straight into a SoftwareBitmap. This bypasses the
    // WIC PNG encode/decode round-trip (which failed with WINCODEC_ERR_COMPONENTNOTFOUND
    // and cost a full encode+decode per frame). System.Drawing's 32bppArgb is BGRA
    // in memory, matching BitmapPixelFormat.Bgra8.
    private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var length = data.Stride * bitmap.Height;
            var buffer = new byte[length];
            Marshal.Copy(data.Scan0, buffer, 0, length);
            var software = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Premultiplied);
            software.CopyFromBuffer(buffer.AsBuffer());
            return software;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}

public sealed record OcrTextResult(string Text, int X, int Y, int Width, int Height);
