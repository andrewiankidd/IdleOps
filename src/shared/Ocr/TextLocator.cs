namespace IdleOps.Shared.Ocr;

/// <summary>
/// Locates target text within recognized words and returns the click point (center
/// of the match). Ported from the WinRT OcrService matching so Windows and Tesseract
/// share identical behaviour: first a single word containing the target, then the
/// shortest run of consecutive words whose joined text contains it.
/// </summary>
public static class TextLocator
{
    public static (int x, int y)? Locate(IReadOnlyList<RecognizedWord> words, string target)
    {
        if (string.IsNullOrEmpty(target)) return null;

        // 1. A single word that contains the target.
        foreach (var w in words)
        {
            if (w.Text.Contains(target, StringComparison.OrdinalIgnoreCase))
                return (w.X + w.Width / 2, w.Y + w.Height / 2);
        }

        // 2. The SHORTEST consecutive span of words whose joined text contains the
        // target (fewest words). A flat word list can cross visual lines, so
        // preferring the tightest run avoids a loose match swallowing a whole row.
        (int x, int y)? best = null;
        var bestLen = int.MaxValue;

        for (var i = 0; i < words.Count; i++)
        {
            var combined = words[i].Text;
            var first = words[i];

            for (var j = i; j < words.Count; j++)
            {
                if (j > i) combined += " " + words[j].Text;

                if (combined.Contains(target, StringComparison.OrdinalIgnoreCase))
                {
                    var len = j - i + 1;
                    if (len < bestLen)
                    {
                        var last = words[j];
                        var left = Math.Min(first.X, last.X);
                        var top = Math.Min(first.Y, last.Y);
                        var right = Math.Max(first.X + first.Width, last.X + last.Width);
                        var bottom = Math.Max(first.Y + first.Height, last.Y + last.Height);
                        best = (left + (right - left) / 2, top + (bottom - top) / 2);
                        bestLen = len;
                    }
                    break; // shortest span starting at i found; longer j only grows it
                }
            }
        }

        return best;
    }
}
