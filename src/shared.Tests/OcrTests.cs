using System.Collections.Generic;
using IdleOps.Shared.Ocr;
using Xunit;

namespace IdleOps.Shared.Tests;

/// <summary>
/// Cross-platform OCR logic tests: text location and Tesseract TSV parsing. Both are
/// pure (no engine, no display), so they run in CI on Windows, Linux and macOS.
/// </summary>
public class OcrTests
{
    private static readonly List<RecognizedWord> Words =
    [
        new("File", 10, 10, 40, 20),
        new("Edit", 60, 10, 40, 20),
        new("Save", 10, 40, 40, 20),
        new("As...", 55, 40, 40, 20),
    ];

    [Fact]
    public void Locate_SingleWord_ReturnsCenter()
    {
        var pt = TextLocator.Locate(Words, "Edit");
        Assert.Equal((80, 20), pt);   // 60 + 40/2, 10 + 20/2
    }

    [Fact]
    public void Locate_IsCaseInsensitive()
    {
        Assert.NotNull(TextLocator.Locate(Words, "file"));
    }

    [Fact]
    public void Locate_ConsecutiveSpan_SpansBothWords()
    {
        // "Save As" spans two words -> center of their combined bounds.
        var pt = TextLocator.Locate(Words, "Save As");
        Assert.NotNull(pt);
        Assert.InRange(pt!.Value.x, 10, 95);
        Assert.InRange(pt.Value.y, 40, 60);
    }

    [Fact]
    public void Locate_Missing_ReturnsNull()
    {
        Assert.Null(TextLocator.Locate(Words, "Nonexistent"));
    }

    [Fact]
    public void Locate_EmptyTarget_ReturnsNull()
    {
        Assert.Null(TextLocator.Locate(Words, ""));
    }

    [Fact]
    public void ParseTsv_KeepsWordRows_WithBoxes()
    {
        // Header + one block/line row (level<5, skipped) + two word rows (level 5).
        var tsv = string.Join('\n',
            "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext",
            "2\t1\t1\t0\t0\t0\t0\t0\t100\t40\t-1\t",
            "5\t1\t1\t1\t1\t1\t10\t12\t40\t18\t96\tFile",
            "5\t1\t1\t1\t1\t2\t60\t12\t40\t18\t95\tEdit",
            "5\t1\t1\t1\t1\t3\t0\t0\t0\t0\t0\t   ");   // blank text -> dropped

        var words = TesseractTextRecognizer.ParseTsv(tsv);
        Assert.Equal(2, words.Count);
        Assert.Equal("File", words[0].Text);
        Assert.Equal(10, words[0].X);
        Assert.Equal(18, words[0].Height);
        Assert.Equal("Edit", words[1].Text);
    }
}
