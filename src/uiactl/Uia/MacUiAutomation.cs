using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IdleOps.Shared.Windows.Uia;

namespace uiactl.Uia;

/// <summary>
/// macOS accessibility backend via AppleScript UI scripting (`osascript` +
/// System Events), the closest built-in analog to UIA/AT-SPI. Addresses a UI element
/// by its accessible name (and optional role) within the window matching the pattern.
///
/// UNVERIFIED and the shakiest of the write-blind backends: AppleScript UI-element
/// addressing is finicky, `entire contents` is slow, and it needs Accessibility
/// permission granted. element-at is not supported here. Treat as a starting point to
/// validate/tune on a Mac, not as known-good.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacUiAutomation : IUiAutomation
{
    public string Name => "osascript (System Events)";

    // UIA control-type name -> AX role (best-effort).
    private static readonly Dictionary<string, string> RoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Button"] = "AXButton", ["Edit"] = "AXTextField", ["Text"] = "AXStaticText",
        ["CheckBox"] = "AXCheckBox", ["RadioButton"] = "AXRadioButton", ["MenuItem"] = "AXMenuItem",
        ["ComboBox"] = "AXComboBox", ["List"] = "AXList", ["Tab"] = "AXTabGroup", ["Slider"] = "AXSlider",
    };

    public ElementInfo? ElementAt(int x, int y) => null; // not supported via osascript

    public DumpResult? Dump(string window, int max)
    {
        var script = Wrap(window, "set out to \"\"\n    repeat with e in (entire contents of win)\n      try\n        set out to out & (role of e) & \"\\t\" & (name of e) & \"\\n\"\n      end try\n    end repeat\n    return out");
        var (ok, stdout, _) = Run(script);
        if (!ok) return null;
        var els = stdout.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Take(max)
            .Select(l => l.Split('\t'))
            .Where(p => p.Length == 2)
            .Select(p => new ElementInfo(p[0], null, p[1], []))
            .ToList();
        return new DumpResult(els.Count, els);
    }

    public UiaResult Invoke(string window, Selector selector) => Act(window, selector, "click el");
    public UiaResult Toggle(string window, Selector selector) => Act(window, selector, "click el");
    public UiaResult Select(string window, Selector selector) => Act(window, selector, "click el");
    public UiaResult ExpandCollapse(string window, Selector selector, bool expand) => Act(window, selector, "click el");

    public UiaResult SetValue(string window, Selector selector, string value) =>
        Act(window, selector, $"set value of el to \"{Escape(value)}\"");

    public UiaResult GetValue(string window, Selector selector)
    {
        var (ok, stdout, stderr) = Run(FindElementScript(window, selector, "return (value of el) as string"));
        return ok ? UiaResult.WithValue(stdout.Trim()) : UiaResult.Fail(Clean(stderr));
    }

    private UiaResult Act(string window, Selector selector, string action)
    {
        var (ok, _, stderr) = Run(FindElementScript(window, selector, action + "\n    return \"ok\""));
        return ok ? UiaResult.Done("ok (osascript)") : UiaResult.Fail(Clean(stderr));
    }

    // Build a script that locates the window, finds the target element, then runs body.
    private static string FindElementScript(string window, Selector selector, string body)
    {
        var name = selector.Name ?? selector.AutomationId;
        var role = selector.ControlType is int ct && RoleMap.TryGetValue(ControlTypes.Name(ct), out var r) ? r : null;
        var cond = name is not null ? $"whose name is \"{Escape(name)}\"" : "";
        if (role is not null) cond = name is not null ? $"{cond} and role is \"{role}\"" : $"whose role is \"{role}\"";
        var finder = $"set el to first UI element of (entire contents of win) {cond}";
        return Wrap(window, $"{finder}\n    {body}");
    }

    private static string Wrap(string window, string body)
    {
        var needle = window.Trim('*').Replace("\"", "\\\"");
        return $$"""
            tell application "System Events"
              repeat with p in (every process whose background only is false)
                repeat with win in (every window of p)
                  if name of win contains "{{needle}}" then
                    tell p
                      {{body}}
                    end tell
                  end if
                end repeat
              end repeat
            end tell
            """;
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Clean(string stderr) =>
        string.Join(" ", stderr.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)).Trim() is { Length: > 0 } s
            ? s : "osascript: element not found or action failed";

    private static (bool ok, string stdout, string stderr) Run(string script)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add(script);
            using var p = Process.Start(psi);
            if (p is null) return (false, "", "failed to start osascript");
            var so = p.StandardOutput.ReadToEnd();
            var se = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode == 0, so, se);
        }
        catch (Exception ex) { return (false, "", ex.Message); }
    }
}
