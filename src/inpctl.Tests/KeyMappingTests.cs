using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

public class KeyMappingTests
{
    [Theory]
    [InlineData("CTRL", 0x11)]
    [InlineData("ALT", 0x12)]
    [InlineData("SHIFT", 0x10)]
    [InlineData("F4", 0x73)]
    public void TryMapKey_KnownTokens_Succeed(string token, ushort expected)
    {
        var ok = InputSender.TryMapKeyForTests(token, out var code);
        Assert.True(ok);
        Assert.Equal(expected, code);
    }
}
