using System.Numerics;

namespace imgfnd.Matching;

/// <summary>A grayscale image as a flat row-major float array (0-255).</summary>
internal sealed record GrayImage(float[] Pixels, int Width, int Height);

/// <summary>Best template-match location and its normalized-correlation confidence (0..1).</summary>
internal readonly record struct MatchResult(int X, int Y, double Confidence);

/// <summary>
/// Pure-managed template matching — normalized cross-correlation (equivalent to
/// OpenCV's TM_CCOEFF_NORMED) on grayscale images, so imgfnd needs no native OpenCV.
/// Integral images give O(1) per-window normalization; the correlation numerator is
/// SIMD-accelerated. Fast enough for the small/medium templates UI automation uses.
/// </summary>
internal static class TemplateMatcher
{
    public static MatchResult Match(GrayImage image, GrayImage template)
    {
        int W = image.Width, H = image.Height, w = template.Width, h = template.Height;
        if (w > W || h > H) return new MatchResult(0, 0, 0);

        var img = image.Pixels;
        int n = w * h;

        // Zero-mean template and its sum of squares (the correlation is invariant to
        // the image window mean because the template is zero-mean).
        var tpl = template.Pixels;
        double tMean = 0;
        for (int i = 0; i < n; i++) tMean += tpl[i];
        tMean /= n;
        var tz = new float[n];
        double tzSumSq = 0;
        for (int i = 0; i < n; i++) { var v = (float)(tpl[i] - tMean); tz[i] = v; tzSumSq += (double)v * v; }
        if (tzSumSq < 1e-9) return new MatchResult(0, 0, 0); // flat template: undefined correlation

        // Integral images of I and I^2 (sizes (W+1)x(H+1)) for O(1) window sum/sumsq.
        var (sum, sumSq) = BuildIntegrals(img, W, H);

        int outW = W - w + 1, outH = H - h + 1;
        double best = double.NegativeInfinity;
        int bestX = 0, bestY = 0;

        for (int y = 0; y < outH; y++)
        {
            for (int x = 0; x < outW; x++)
            {
                double winSum = WindowSum(sum, W, x, y, w, h);
                double winSumSq = WindowSum(sumSq, W, x, y, w, h);
                double denomImg = winSumSq - winSum * winSum / n; // Σ(I-mean)^2 over the window
                if (denomImg <= 1e-9) continue;                    // flat window

                double numerator = Correlate(img, W, x, y, tz, w, h);
                double score = numerator / Math.Sqrt(tzSumSq * denomImg);
                if (score > best) { best = score; bestX = x; bestY = y; }
            }
        }

        return new MatchResult(bestX, bestY, double.IsFinite(best) ? Math.Clamp(best, -1.0, 1.0) : 0);
    }

    // Σ tz(i,j) * I(x+i, y+j) over the template window, SIMD over each row.
    private static double Correlate(float[] img, int W, int x, int y, float[] tz, int w, int h)
    {
        double sum = 0;
        int vw = Vector<float>.Count;
        for (int j = 0; j < h; j++)
        {
            int imgRow = (y + j) * W + x;
            int tplRow = j * w;
            int i = 0;
            var acc = Vector<float>.Zero;
            for (; i <= w - vw; i += vw)
            {
                var a = new Vector<float>(img, imgRow + i);
                var b = new Vector<float>(tz, tplRow + i);
                acc += a * b;
            }
            sum += Vector.Dot(acc, Vector<float>.One);
            for (; i < w; i++) sum += img[imgRow + i] * tz[tplRow + i];
        }
        return sum;
    }

    // Sum over the w×h window at (x,y) from a (W+1)-stride integral image.
    private static double WindowSum(double[] integral, int W, int x, int y, int w, int h)
    {
        int stride = W + 1;
        int a = y * stride + x;
        int b = y * stride + (x + w);
        int c = (y + h) * stride + x;
        int d = (y + h) * stride + (x + w);
        return integral[d] - integral[b] - integral[c] + integral[a];
    }

    private static (double[] sum, double[] sumSq) BuildIntegrals(float[] img, int W, int H)
    {
        int stride = W + 1;
        var sum = new double[stride * (H + 1)];
        var sumSq = new double[stride * (H + 1)];
        for (int y = 0; y < H; y++)
        {
            double rowSum = 0, rowSumSq = 0;
            for (int x = 0; x < W; x++)
            {
                float v = img[y * W + x];
                rowSum += v; rowSumSq += (double)v * v;
                int idx = (y + 1) * stride + (x + 1);
                sum[idx] = sum[y * stride + (x + 1)] + rowSum;
                sumSq[idx] = sumSq[y * stride + (x + 1)] + rowSumSq;
            }
        }
        return (sum, sumSq);
    }
}
