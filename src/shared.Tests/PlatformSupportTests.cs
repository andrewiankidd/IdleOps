using IdleOps.Shared.Platform;
using Xunit;

namespace IdleOps.Shared.Tests;

public class PlatformSupportTests
{
    [Fact]
    public void EnsureWindows_MatchesCurrentPlatform()
    {
        Assert.Equal(OperatingSystem.IsWindows(), PlatformSupport.EnsureWindows("test"));
    }

    [Fact]
    public void RequireWindows_ThrowsOffWindowsOnly()
    {
        if (OperatingSystem.IsWindows())
        {
            PlatformSupport.RequireWindows("test"); // must not throw on Windows
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => PlatformSupport.RequireWindows("test"));
        }
    }
}
