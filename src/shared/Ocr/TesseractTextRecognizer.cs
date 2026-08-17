using System.Globalization;
using IdleOps.Shared.Capture;

namespace IdleOps.Shared.Ocr;

/// <summary>
/// Cross-platform OCR via the Tesseract CLI (`tesseract img stdout --psm 11 tsv`).
/// Parses the TSV word rows into <see cref="RecognizedWord"/>s. Needs `tesseract` on
/// PATH (Linux: `apt install tesseract-ocr`; macOS: `brew install tesseract`).
/// </summary>
public sealed class TesseractTextRecognizer : ITextRecognizer
{
    public string Name => "tesseract";

    public Task<IReadOnlyList<RecognizedWord>> RecognizeAsync(string imagePath)
    {
        // --psm 11 = "sparse text": find as much text as possible, no layout
        // assumptions — a good fit for scattered UI labels.
        var (ok, stdout, stderr) = ProcessRunner.Run("tesseract", imagePath, "stdout", "--psm", "11", "tsv");
        if (!ok)
        {
            Console.Error.WriteLine($"[txtfnd] tesseract failed: {stderr.Trim()} (is tesseract-ocr installed?)");
            return Task.FromResult<IReadOnlyList<RecognizedWord>>([]);
        }
        return Task.FromResult(ParseTsv(stdout));
    }

    // Tesseract TSV columns:
    // level page_num block_num par_num line_num word_num left top width height conf text
    // Word rows are level 5; keep those with non-empty text.
    internal static IReadOnlyList<RecognizedWord> ParseTsv(string tsv)
    {
        var words = new List<RecognizedWord>();
        var lines = tsv.Replace("\r\n", "\n").Split('\n');
        for (var i = 1; i < lines.Length; i++) // row 0 is the header
        {
            var cols = lines[i].Split('\t');
            if (cols.Length < 12) continue;
            if (cols[0] != "5") continue; // level 5 = word

            var text = cols[11].Trim();
            if (text.Length == 0) continue;

            if (int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var left) &&
                int.TryParse(cols[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var top) &&
                int.TryParse(cols[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var w) &&
                int.TryParse(cols[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
            {
                words.Add(new RecognizedWord(text, left, top, w, h));
            }
        }
        return words;
    }
}
