using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

/// <summary>
/// Offline tests for the macOS (cliclick) chord translation. The runtime injection
/// needs a Mac with cliclick + Accessibility permission and is verified there; the
/// mapping is pure and tested here.
/// </summary>
public class MacKeysTests
{
    [Fact]
    public void Chord_LetterKey_WrapsInModifierDownUp()
    {
        Assert.Equal(new[] { "kd:ctrl", "t:s", "ku:ctrl" }, MacKeys.Translate("CTRL+S"));
    }

    [Fact]
    public void WinAndSuper_MapToCommand()
    {
        Assert.Equal(new[] { "kd:cmd", "t:c", "ku:cmd" }, MacKeys.Translate("WIN+C"));
        Assert.Equal(new[] { "kd:cmd", "t:v", "ku:cmd" }, MacKeys.Translate("SUPER+V"));
    }

    [Fact]
    public void NamedKey_UsesKeyPress()
    {
        Assert.Equal(new[] { "kp:return" }, MacKeys.Translate("ENTER"));
        Assert.Equal(new[] { "kp:arrow-left" }, MacKeys.Translate("LEFT"));
    }

    [Fact]
    public void CommaSequence_Concatenates()
    {
        Assert.Equal(new[] { "kd:ctrl", "t:a", "ku:ctrl", "kp:fwd-delete" }, MacKeys.Translate("CTRL+A, DELETE"));
    }
}
