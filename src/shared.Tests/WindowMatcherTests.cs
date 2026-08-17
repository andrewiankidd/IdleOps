using IdleOps.Shared.Windows;
using Xunit;

namespace IdleOps.Shared.Tests;

public class WindowMatcherTests
{
    [Theory]
    [InlineData("Rick Astley - Notepad", "Rick Astley*")]
    [InlineData("Rick Astley - Notepad", "*Astley*")]
    [InlineData("Rick Astley - Notepad", "R*Astley*pad")]
    [InlineData("Chrome - YouTube", "*YouTube")]
    [InlineData("My Application", "My Application")]
    [InlineData("Test 123", "*123")]
    [InlineData("Test 123", "Test*")]
    [InlineData("Some Long Window Title Here", "*Window*Here")]
    public void WildcardRegex_Matches(string title, string pattern)
    {
        var regex = WindowMatcher.BuildWildcardRegex(pattern);
        Assert.Matches(regex, title);
    }

    [Theory]
    [InlineData("Other Window", "Rick Astley*")]
    [InlineData("Chrome", "Firefox*")]
    [InlineData("Abc", "Def")]
    [InlineData("Test", "Test2*")]
    public void WildcardRegex_DoesNotMatch(string title, string pattern)
    {
        var regex = WindowMatcher.BuildWildcardRegex(pattern);
        Assert.DoesNotMatch(regex, title);
    }

    [Fact]
    public void WildcardRegex_CaseInsensitive()
    {
        var regex = WindowMatcher.BuildWildcardRegex("notepad*");
        Assert.Matches(regex, "Notepad - Untitled");
        Assert.Matches(regex, "NOTEPAD - Test");
    }

    [Fact]
    public void WildcardRegex_EscapesSpecialChars()
    {
        var regex = WindowMatcher.BuildWildcardRegex("Test (1)");
        Assert.Matches(regex, "Test (1)");
        Assert.DoesNotMatch(regex, "Test 1");
    }

    [Fact]
    public void WildcardRegex_SingleStar_MatchesAnything()
    {
        var regex = WindowMatcher.BuildWildcardRegex("*");
        Assert.Matches(regex, "Anything At All");
        Assert.Matches(regex, "");
    }

    [WindowsOnlyFact]
    public void GetWindowTitle_NullHandle_ReturnsEmpty()
    {
        var title = WindowMatcher.GetWindowTitle(IntPtr.Zero);
        Assert.Equal(string.Empty, title);
    }

    [WindowsOnlyFact]
    public void FindWindow_NonexistentPattern_ReturnsNull()
    {
        var result = WindowMatcher.FindWindow("ZZZZZ_NonexistentWindow_12345_ZZZZZ");
        Assert.Null(result);
    }

    [WindowsOnlyFact]
    public void FindAllWindows_NonexistentPattern_ReturnsEmpty()
    {
        var results = WindowMatcher.FindAllWindows("ZZZZZ_NonexistentWindow_12345_ZZZZZ");
        Assert.Empty(results);
    }
}
