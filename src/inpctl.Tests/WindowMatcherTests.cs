using IdleOps.Shared.Windows;
using Xunit;

namespace inpctl.Tests;

public class WindowMatcherTests
{
    [Theory]
    [InlineData("Rick Astley - Notepad", "Rick Astley*")]
    [InlineData("Rick Astley - Notepad", "*Astley*")]
    [InlineData("Rick Astley - Notepad", "R*Astley*pad")]
    [InlineData("Chrome - YouTube", "*YouTube")]
    public void WildcardPatterns_MatchExpected(string title, string pattern)
    {
        var regex = WindowMatcher.BuildWildcardRegex(pattern);
        Assert.Matches(regex, title);
    }

    [Fact]
    public void WildcardPatterns_NonMatch()
    {
        var regex = WindowMatcher.BuildWildcardRegex("Rick Astley*");
        Assert.DoesNotMatch(regex, "Other Window");
    }
}
