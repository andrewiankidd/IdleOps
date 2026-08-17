using IdleOps.Shared.Windowing;
using Xunit;

namespace IdleOps.Shared.Tests;

public class WindowingTests
{
    [Fact]
    public void Factory_ReturnsLocator_OnWindowsAndLinux()
    {
        var locator = WindowLocatorFactory.Create();
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            Assert.NotNull(locator);   // user32 or xdotool backend
        else
            Assert.Null(locator);      // macOS: window enumeration by title not wired up
    }

    [Fact]
    public void Nonexistent_Window_DoesNotExist()
    {
        var locator = WindowLocatorFactory.Create();
        if (locator is null) return;   // macOS: nothing to assert
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
