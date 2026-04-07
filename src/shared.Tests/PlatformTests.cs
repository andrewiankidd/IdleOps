using IdleOps.Shared.Platform;
using Xunit;

namespace IdleOps.Shared.Tests;

public class PlatformTests
{
    [Fact]
    public void DescribeMapsKnownValues()
    {
        Assert.Equal("Windows", HostInfo.Describe(HostPlatform.Windows));
        Assert.Equal("macOS", HostInfo.Describe(HostPlatform.MacOS));
        Assert.Equal("Linux", HostInfo.Describe(HostPlatform.Linux));
        Assert.Equal("Unknown", HostInfo.Describe(HostPlatform.Unknown));
    }

    [Fact]
    public void DetectReturnsDefinedEnum()
    {
        var detected = HostInfo.Detect();
        Assert.True(Enum.IsDefined(typeof(HostPlatform), detected));
    }
}
