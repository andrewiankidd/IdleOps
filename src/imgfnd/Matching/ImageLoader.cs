using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace imgfnd.Matching;

/// <summary>Loads any image file (PNG/JPG/BMP) as an 8-bit grayscale float buffer via ImageSharp.</summary>
internal static class ImageLoader
{
    public static GrayImage Load(string path)
    {
        using var image = Image.Load<L8>(path);   // decode + convert to 8-bit luminance
        int w = image.Width, h = image.Height;
        var px = new float[w * h];
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < w; x++) px[y * w + x] = row[x].PackedValue;
            }
        });
        return new GrayImage(px, w, h);
    }
}
