using imgfnd.Matching;
using Xunit;

namespace imgfnd.Tests;

public class TemplateMatcherTests
{
    // A high-entropy (non-periodic) block so the correlation peak is sharp/unique.
    private static float Pattern(int i, int j) => (float)(((uint)(i * 73856093) ^ (uint)(j * 19349663)) % 251);

    [Fact]
    public void Match_FindsTemplateAtKnownLocation()
    {
        const int W = 140, H = 100, tw = 14, th = 12, tx = 55, ty = 33;
        var img = new float[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                img[y * W + x] = (x * 3 + y * 2) % 200;   // smooth background

        var tpl = new float[tw * th];
        for (int j = 0; j < th; j++)
            for (int i = 0; i < tw; i++)
            {
                var v = Pattern(i, j);
                img[(ty + j) * W + (tx + i)] = v;   // stamp the template into the image
                tpl[j * tw + i] = v;
            }

        var r = TemplateMatcher.Match(new GrayImage(img, W, H), new GrayImage(tpl, tw, th));

        Assert.Equal(tx, r.X);
        Assert.Equal(ty, r.Y);
        Assert.True(r.Confidence > 0.99, $"confidence was {r.Confidence:0.###}");
    }

    [Fact]
    public void Match_FlatTemplate_YieldsZeroConfidence()
    {
        var img = new float[50 * 50];
        for (int i = 0; i < img.Length; i++) img[i] = i % 100;
        var flat = new float[8 * 8]; // all zeros -> undefined correlation
        var r = TemplateMatcher.Match(new GrayImage(img, 50, 50), new GrayImage(flat, 8, 8));
        Assert.Equal(0, r.Confidence);
    }

    [Fact]
    public void Match_TemplateLargerThanImage_ReturnsZero()
    {
        var r = TemplateMatcher.Match(new GrayImage(new float[4], 2, 2), new GrayImage(new float[16], 4, 4));
        Assert.Equal(0, r.Confidence);
    }
}
