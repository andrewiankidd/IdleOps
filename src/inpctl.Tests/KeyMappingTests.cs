using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

public class KeyMappingTests
{
    [WindowsOnlyTheory]
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

    [WindowsOnlyTheory]
    [InlineData("F", 0x46)]
    [InlineData("a", 0x41)]
    [InlineData("1", 0x31)]
    public void TryMapKey_SingleChar_ReturnsVirtualKeyOnly(string token, ushort expected)
    {
        // The shift-state high byte from VkKeyScan must be stripped — the result is
        // used as a virtual-key code for SendInput/PostMessage.
        Assert.True(InputSender.TryMapKeyForTests(token, out var code));
        Assert.Equal(expected, code);
    }
}
