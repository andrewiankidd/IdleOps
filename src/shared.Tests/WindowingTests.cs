using IdleOps.Shared.Windowing;
using Xunit;

namespace IdleOps.Shared.Tests;

public class WindowingTests
{
    // Windows (user32), Linux (xdotool) and macOS (osascript) all have a backend now;
    // only an unrecognized OS returns null.
    [Fact]
    public void Factory_ReturnsLocator_OnEverySupportedOs()
    {
        var locator = WindowLocatorFactory.Create();
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            Assert.NotNull(locator);
        else
            Assert.Null(locator);
    }

    [Fact]
    public void Nonexistent_Window_DoesNotExist()
    {
        var locator = WindowLocatorFactory.Create();
        if (locator is null) return;   // unsupported OS: nothing to assert
        Assert.False(locator.Exists("ZZZ_no_such_window_zzz_12345"));
    }

    [Theory]
    [InlineData("*Notepad*", ".*Notepad.*")]
    [InlineData("Untitled - Notepad", "Untitled\\ -\\ Notepad")]
    public void ToRegex_ConvertsWildcardAndEscapes(string pattern, string expected)
    {
        Assert.Equal(expected, LinuxX11Windows.ToRegex(pattern));
    }
}
