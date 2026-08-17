using IdleOps.Shared.Capture;
using Xunit;

namespace scrcap.Tests;

public class ScreenCapturerFactoryTests
{
    [Theory]
    [InlineData("root")]
    [InlineData("screen")]
    [InlineData("desktop")]
    [InlineData("*")]
    public void IsWholeScreen_TrueForDisplayAliases(string pattern)
    {
        Assert.True(ScreenCapturerFactory.IsWholeScreen(pattern));
    }

    [Theory]
    [InlineData("Notepad")]
    [InlineData("*Firefox*")]
    [InlineData("Untitled - Notepad")]
    public void IsWholeScreen_FalseForWindowPatterns(string pattern)
    {
        Assert.False(ScreenCapturerFactory.IsWholeScreen(pattern));
    }

    [Fact]
    public void Create_ReturnsABackendForThisOs()
    {
        // Windows/Linux/macOS all have an implementation; this host is one of them.
        Assert.NotNull(ScreenCapturerFactory.Create());
    }
}
