using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

/// <summary>
/// Offline tests for the Linux (xdotool) backend's pure translation logic — chord
/// mapping, window-search regex, and mouse-coordinate parsing. The runtime
/// injection needs a live X11 session and is verified there.
/// </summary>
public class XdotoolKeysTests
{
    [Theory]
    [InlineData("CTRL+S", "ctrl+s")]
    [InlineData("WIN+D", "super+d")]
    [InlineData("ALT+F4", "alt+F4")]
    [InlineData("CTRL+SHIFT+ESCAPE", "ctrl+shift+Escape")]
    [InlineData("ENTER", "Return")]
    [InlineData("PAGEUP", "Prior")]
    public void Translate_MapsChordToXdotoolSpec(string chord, string expected)
    {
        Assert.Equal(new[] { expected }, XdotoolKeys.Translate(chord));
    }

    [Fact]
    public void Translate_CommaSequence_YieldsSeparateSpecs()
    {
        Assert.Equal(new[] { "ctrl+a", "Delete" }, XdotoolKeys.Translate("CTRL+A, DELETE"));
    }

    [Fact]
    public void MapToken_SingleLetter_IsLowercased()
    {
        // So "CTRL+S" -> ctrl+s, not ctrl+shift+s.
        Assert.Equal("s", XdotoolKeys.MapToken("S"));
    }


    [Fact]
    public void ParseMousePoints_AbsolutePixels()
    {
        Assert.True(MouseCoords.TryParse("10,20", bounds: null, out var pts));
        Assert.Equal(new[] { (10, 20) }, pts);
    }

    [Fact]
    public void ParseMousePoints_Percentage_UsesWindowSpan()
    {
        var bounds = new WindowBounds(0, 0, 200, 100);
        Assert.True(MouseCoords.TryParse("50%,50%", bounds, out var pts));
        Assert.Equal(new[] { (100, 50) }, pts);
    }

    [Fact]
    public void ParseMousePoints_Drag_YieldsTwoPoints()
    {
        Assert.True(MouseCoords.TryParse("0,0-10,10", bounds: null, out var pts));
        Assert.Equal(new[] { (0, 0), (10, 10) }, pts);
    }

    [Fact]
    public void ParseMousePoints_Invalid_ReturnsFalse()
    {
        Assert.False(MouseCoords.TryParse("nope", bounds: null, out _));
    }
}
