using System.Linq;
using playbk.Execution;
using playbk.Model;
using Xunit;

namespace playbk.Tests;

/// <summary>
/// Static, hardware-free validation of runbooks against a device profile — the
/// "complain loudly about bad combinations" layer. No desktop or transport needed.
/// </summary>
public class RunbookValidatorTests
{
    private static Script Of(params Step[] steps) => new() { Steps = steps.ToList() };

    [Fact]
    public void Local_AllowsEverything_IncludingUiaAndExec()
    {
        var script = Of(
            new Step { Name = "launch", Action = "exec", Args = "notepad.exe" },
            new Step { Name = "press save", Action = "invoke", Window = "Notepad", Element = "Save" },
            new Step { Name = "wait", Action = "wait-window", Window = "Notepad" });

        Assert.Empty(RunbookValidator.Validate(script, DeviceProfile.Local));
    }

    [Fact]
    public void Offbox_RejectsUiaVerb_WithAClearReason()
    {
        var script = Of(new Step { Name = "press save", Action = "invoke", Window = "app", Element = "Save" });
        var violations = RunbookValidator.Validate(script, DeviceProfile.Offbox);

        var v = Assert.Single(violations);
        Assert.Equal(1, v.StepNumber);
        Assert.Contains("UI Automation", v.Reason);
    }

    [Fact]
    public void Offbox_RejectsExec_NoProcessControlOnTargetMachine()
    {
        var script = Of(new Step { Name = "launch", Action = "exec", Args = "notepad.exe" });
        var violations = RunbookValidator.Validate(script, DeviceProfile.Offbox);
        Assert.Contains(violations, x => x.Reason.Contains("process"));
    }

    [Fact]
    public void Offbox_RejectsTitleWait_ButAllowsTextWait()
    {
        var titleWait = Of(new Step { Name = "w", Action = "wait-window", Window = "Notepad" });
        Assert.NotEmpty(RunbookValidator.Validate(titleWait, DeviceProfile.Offbox));

        var textWait = Of(new Step { Name = "w", Action = "wait-window", Window = "Notepad", Text = "Ready" });
        Assert.Empty(RunbookValidator.Validate(textWait, DeviceProfile.Offbox));
    }

    [Fact]
    public void Offbox_RejectsBackgroundDelivery_NeedsAWindowHandle()
    {
        var bg = Of(new Step { Name = "type", Action = "type", Window = "app", Text = "hi", Background = true });
        Assert.Contains(RunbookValidator.Validate(bg, DeviceProfile.Offbox), x => x.Reason.Contains("window handle"));

        var fg = Of(new Step { Name = "type", Action = "type", Window = "app", Text = "hi" });
        Assert.Empty(RunbookValidator.Validate(fg, DeviceProfile.Offbox));
    }

    [Fact]
    public void Offbox_AllowsVisionAndInputActions()
    {
        var script = Of(
            new Step { Name = "shot", Action = "screenshot", Window = "feed", Output = "s.png" },
            new Step { Name = "click", Action = "click-text", Window = "feed", Text = "OK" },
            new Step { Name = "keys", Action = "keyboard", Window = "feed", Text = "CTRL+S" },
            new Step { Name = "type", Action = "type", Window = "feed", Text = "hello" },
            new Step { Name = "wait", Action = "sleep", Timeout = 1 });

        Assert.Empty(RunbookValidator.Validate(script, DeviceProfile.Offbox));
    }

    [Fact]
    public void Offbox_ReportsEveryOffendingStep()
    {
        var script = Of(
            new Step { Name = "ok", Action = "click-text", Window = "feed", Text = "OK" },
            new Step { Name = "bad1", Action = "invoke", Window = "feed", Element = "Save" },
            new Step { Name = "bad2", Action = "exec", Args = "notepad.exe" });

        var violations = RunbookValidator.Validate(script, DeviceProfile.Offbox);
        Assert.Equal(new[] { 2, 3 }, violations.Select(v => v.StepNumber).Distinct().ToArray());
    }

    [Fact]
    public void UnknownProfile_ResolvesToNull()
    {
        Assert.Null(DeviceProfile.Resolve("teleport"));
        Assert.NotNull(DeviceProfile.Resolve("offbox"));
        Assert.NotNull(DeviceProfile.Resolve("LOCAL")); // case-insensitive
    }
}
