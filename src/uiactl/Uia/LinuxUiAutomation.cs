using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IdleOps.Shared.Windows.Uia;

namespace uiactl.Uia;

/// <summary>
/// Linux backend: drives the AT-SPI2 accessibility tree via the bundled
/// atspi_helper.py (pyatspi). Maps the UIA-flavoured selector (name + control type)
/// onto AT-SPI names/roles. Needs python3-pyatspi and a running accessibility bus.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxUiAutomation : IUiAutomation
{
    public string Name => "at-spi2";

    // UIA control-type name -> an AT-SPI role substring (matched loosely by the helper).
    private static readonly Dictionary<string, string> RoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Button"] = "button", ["Edit"] = "text", ["Text"] = "label", ["CheckBox"] = "check",
        ["RadioButton"] = "radio", ["MenuItem"] = "menu item", ["Menu"] = "menu", ["List"] = "list",
        ["ListItem"] = "list item", ["ComboBox"] = "combo", ["Tab"] = "page tab", ["TabItem"] = "page tab",
        ["Tree"] = "tree", ["TreeItem"] = "tree item", ["Slider"] = "slider", ["Window"] = "frame",
        ["Document"] = "document", ["Group"] = "panel", ["ToolBar"] = "tool bar",
    };

    private static string HelperPath => Path.Combine(AppContext.BaseDirectory, "atspi_helper.py");

    public ElementInfo? ElementAt(int x, int y)
    {
        var (ok, stdout, _) = Run("element-at", "", null, $"{x},{y}");
        return ok ? ParseElementLine(stdout) : null;
    }

    public DumpResult? Dump(string window, int max)
    {
        var (ok, stdout, _) = Run("dump", window, null, max: max);
        if (!ok) return null;
        var els = stdout.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseElementLine).Where(e => e is not null).Select(e => e!).ToList();
        return new DumpResult(els.Count, els);
    }

    public UiaResult SetValue(string window, Selector selector, string value) => Action("set-value", window, selector, value);
    public UiaResult GetValue(string window, Selector selector)
    {
        var (ok, stdout, stderr) = Run("get-value", window, selector);
        return ok ? UiaResult.WithValue(stdout.Trim()) : UiaResult.Fail(Clean(stderr));
    }
    public UiaResult Invoke(string window, Selector selector) => Action("invoke", window, selector);
    public UiaResult Toggle(string window, Selector selector) => Action("toggle", window, selector);
    public UiaResult ExpandCollapse(string window, Selector selector, bool expand) => Action(expand ? "expand" : "collapse", window, selector);
    public UiaResult Select(string window, Selector selector) => Action("select", window, selector);

    private UiaResult Action(string verb, string window, Selector selector, string? value = null)
    {
        var (ok, _, stderr) = Run(verb, window, selector, value: value);
        return ok ? UiaResult.Done($"{verb} ok (at-spi2)") : UiaResult.Fail(Clean(stderr));
    }

    private static ElementInfo? ParseElementLine(string line)
    {
        // "[role] name="the name""
        var m = Regex.Match(line, "\\[(?<role>[^\\]]+)\\]\\s*name=\"(?<name>.*)\"\\s*$");
        if (!m.Success) return null;
        return new ElementInfo(m.Groups["role"].Value.Trim(), null, m.Groups["name"].Value, []);
    }

    private (bool ok, string stdout, string stderr) Run(string verb, string window, Selector? selector, string? point = null, string? value = null, int? max = null)
    {
        var args = new List<string> { HelperPath, verb };
        if (!string.IsNullOrEmpty(window)) { args.Add("--window"); args.Add(window); }
        if (selector is { } sel)
        {
            var name = sel.Name ?? sel.AutomationId;   // AT-SPI rarely exposes an id; fall back to name
            if (!string.IsNullOrEmpty(name)) { args.Add("--name"); args.Add(name); }
            if (sel.ControlType is int ct)
            {
                var uiaName = ControlTypes.Name(ct);
                var role = RoleMap.TryGetValue(uiaName, out var r) ? r : uiaName.ToLowerInvariant();
                args.Add("--role"); args.Add(role);
            }
        }
        if (point is not null) { args.Add("--point"); args.Add(point); }
        if (value is not null) { args.Add("--value"); args.Add(value); }
        if (max is int m) { args.Add("--max"); args.Add(m.ToString()); }

        try
        {
            var psi = new ProcessStartInfo { FileName = "python3", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p is null) return (false, "", "failed to start python3");
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode == 0, stdout, stderr);
        }
        catch (Exception ex)
        {
            return (false, "", $"python3/atspi helper failed: {ex.Message} (install python3-pyatspi)");
        }
    }

    private static string Clean(string stderr) =>
        string.Join(" ", stderr.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)).Trim() is { Length: > 0 } s
            ? s : "at-spi2: not found or action failed";
}
