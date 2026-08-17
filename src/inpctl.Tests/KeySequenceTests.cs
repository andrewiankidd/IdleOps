using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

public class KeySequenceTests
{
    private const ushort Ctrl = 0x11, Shift = 0x10, Enter = 0x0D, F1 = 0x70, F2 = 0x71, F4 = 0x73;
    private const ushort Win = 0x5B, D = 0x44;

    [WindowsOnlyFact]
    public void SingleNamedKey_PressThenRelease()
    {
        var seq = InputSender.BuildKeySequenceForTests("ENTER");
        Assert.Equal(new[] { (Enter, false), (Enter, true) }, seq);
    }

    [WindowsOnlyFact]
    public void Combo_WrapsKeyInModifierDownAndUp()
    {
        // CTRL down, F4 down, F4 up, CTRL up
        var seq = InputSender.BuildKeySequenceForTests("CTRL+F4");
        Assert.Equal(new[] { (Ctrl, false), (F4, false), (F4, true), (Ctrl, true) }, seq);
    }

    [WindowsOnlyFact]
    public void MultiModifier_ReleasesInReverseOrder()
    {
        // CTRL down, SHIFT down, F4 down, F4 up, SHIFT up, CTRL up
        var seq = InputSender.BuildKeySequenceForTests("CTRL+SHIFT+F4");
        Assert.Equal(
            new[] { (Ctrl, false), (Shift, false), (F4, false), (F4, true), (Shift, true), (Ctrl, true) },
            seq);
    }

    [WindowsOnlyFact]
    public void WinKey_IsAModifierInAChord()
    {
        // WIN down, D down, D up, WIN up  (e.g. Win+D = show desktop)
        var seq = InputSender.BuildKeySequenceForTests("WIN+D");
        Assert.Equal(new[] { (Win, false), (D, false), (D, true), (Win, true) }, seq);
    }

    [WindowsOnlyFact]
    public void CommaSeparated_ProducesSequentialKeystrokes()
    {
        var seq = InputSender.BuildKeySequenceForTests("F1, F2");
        Assert.Equal(new[] { (F1, false), (F1, true), (F2, false), (F2, true) }, seq);
    }

    [WindowsOnlyFact]
    public void Empty_YieldsEmptySequence()
    {
        Assert.Empty(InputSender.BuildKeySequenceForTests(""));
    }

    [WindowsOnlyFact]
    public void SingleChar_UsesVirtualKey_NotShiftedScanCode()
    {
        // Regression: "F" must map to vk 0x46, not VkKeyScan's 0x146 (shift bit in
        // the high byte). Posting 0x146 as a virtual-key silently fails in targets.
        var seq = InputSender.BuildKeySequenceForTests("F");
        Assert.Equal(new[] { ((ushort)0x46, false), ((ushort)0x46, true) }, seq);
    }

    [WindowsOnlyFact]
    public void UnknownToken_IsSkipped()
    {
        Assert.Empty(InputSender.BuildKeySequenceForTests("NOPE_NOT_A_KEY"));
    }
}
