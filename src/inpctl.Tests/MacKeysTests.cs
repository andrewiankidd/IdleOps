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

    // cliclick's kd:/ku: accept modifiers only — `kd:s` is rejected with a non-zero exit,
    // so a hold request naming an ordinary key has to be refused rather than emitted.
    [Fact]
    public void Hold_Modifiers_AreHoldable()
    {
        Assert.Equal("ctrl", MacKeys.TranslateHold("CTRL"));
        Assert.Equal("cmd", MacKeys.TranslateHold("WIN"));
        Assert.Equal("ctrl,shift", MacKeys.TranslateHold("CTRL+SHIFT"));
    }

    [Fact]
    public void Hold_OrdinaryKey_IsRefused()
    {
        Assert.Null(MacKeys.TranslateHold("W"));
        Assert.Null(MacKeys.TranslateHold("CTRL+S"));   // the key half cannot be held
        Assert.Null(MacKeys.TranslateHold("ENTER"));
        Assert.Null(MacKeys.TranslateHold(""));
    }
}
