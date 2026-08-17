using playbk.Model;

namespace playbk.Execution;

/// <summary>A step that cannot run under the chosen device profile, and why.</summary>
internal sealed record Violation(int StepNumber, string StepName, string Action, string Reason);

/// <summary>
/// Static, pre-flight validation of a runbook against a <see cref="DeviceProfile"/>.
/// Runs before any step executes, so a bad transport/action combination (e.g. a UIA
/// verb over an off-box HID+capture link) fails loudly and completely instead of
/// blowing up mid-run after earlier steps already had side effects.
/// </summary>
internal static class RunbookValidator
{
    /// <summary>The capabilities a step needs, derived from its action and the fields that change delivery.</summary>
    public static Capability RequiredCapabilities(Step step)
    {
        // Background key/text delivery uses PostMessage, which needs a real HWND.
        var background = step.Background || string.Equals(step.Method, "background", StringComparison.OrdinalIgnoreCase);
        var bgHandle = background ? Capability.WindowHandle : Capability.None;

        return step.Action.ToLowerInvariant() switch
        {
            "exec" => Capability.LocalProcess,
            "sleep" => Capability.None,
            "speak" => Capability.None,                     // host-side audio; harmless under any profile

            // A title wait needs a window handle; a text wait is OCR (vision) over the source.
            "wait-window" => string.IsNullOrWhiteSpace(step.Text) ? Capability.WindowHandle : Capability.Vision,

            "screenshot" => Capability.Vision,
            "assert-text" => Capability.Vision,
            "click-text" => Capability.Vision | Capability.Input,

            "type" or "keyboard" or "keys" or "chord" => Capability.Input | bgHandle,
            "hold" => Capability.Input | bgHandle,

            // UI Automation verbs need software access to the target's accessibility tree.
            "set-value" or "invoke" or "toggle" or "expand" or "collapse" or "select" or "assert-value"
                => Capability.Uia,

            _ => Capability.None, // unknown actions are caught at dispatch / by the planner
        };
    }

    /// <summary>Validate every step against the profile; returns one violation per capability a step needs but the profile lacks.</summary>
    public static IReadOnlyList<Violation> Validate(Script script, DeviceProfile profile)
    {
        var violations = new List<Violation>();
        for (var i = 0; i < script.Steps.Count; i++)
        {
            var step = script.Steps[i];
            var missing = RequiredCapabilities(step) & ~profile.Capabilities;
            if (missing == Capability.None) continue;

            foreach (var reason in ReasonsFor(missing, profile))
            {
                violations.Add(new Violation(i + 1, step.Name, step.Action, reason));
            }
        }
        return violations;
    }

    // Each missing capability becomes a human-readable reason that names the fix.
    private static IEnumerable<string> ReasonsFor(Capability missing, DeviceProfile profile)
    {
        if (missing.HasFlag(Capability.Uia))
            yield return $"needs UI Automation, which the '{profile.Name}' profile has no software access to provide. " +
                         "Drive the control visually instead: click-text / imgfnd + a keyboard shortcut.";
        if (missing.HasFlag(Capability.WindowHandle))
            yield return $"needs a target window handle (title match or background delivery), which the '{profile.Name}' profile lacks. " +
                         "Use a text-based wait (wait-window with text:) and foreground input.";
        if (missing.HasFlag(Capability.LocalProcess))
            yield return $"launches a process on the host, but the '{profile.Name}' profile drives a separate machine. " +
                         "Start the target app through its own UI (e.g. the Start menu via keyboard/click-text).";
        if (missing.HasFlag(Capability.Vision))
            yield return $"needs screen capture, which the '{profile.Name}' profile does not provide.";
        if (missing.HasFlag(Capability.Input))
            yield return $"needs synthetic input, which the '{profile.Name}' profile does not provide.";
    }

    /// <summary>Format violations as a loud, multi-line block for the console.</summary>
    public static string Format(IReadOnlyList<Violation> violations, DeviceProfile profile)
    {
        var lines = new List<string>
        {
            $"Runbook is not valid for the '{profile.Name}' profile ({profile.Description}). {violations.Count} problem(s):",
        };
        lines.AddRange(violations.Select(v => $"  - step {v.StepNumber} '{v.StepName}' (action: {v.Action}) {v.Reason}"));
        return string.Join(Environment.NewLine, lines);
    }
}
