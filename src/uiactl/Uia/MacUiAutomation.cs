using System.Diagnostics;
using System.Runtime.Versioning;
using IdleOps.Shared.Platform;
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

    // UIA control-type name -> AX role(s), best-effort. Several UIA types map to more than
    // one AX role — a multi-line editor is AXTextArea, not AXTextField, so matching only
    // the latter misses the main text control of most document apps.
    private static readonly Dictionary<string, string[]> RoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Button"] = ["AXButton"], ["Edit"] = ["AXTextField", "AXTextArea"], ["Text"] = ["AXStaticText"],
        ["CheckBox"] = ["AXCheckBox"], ["RadioButton"] = ["AXRadioButton"], ["MenuItem"] = ["AXMenuItem"],
        ["ComboBox"] = ["AXComboBox", "AXPopUpButton"], ["List"] = ["AXList"], ["Tab"] = ["AXTabGroup"],
        ["Slider"] = ["AXSlider"],
    };

    public ElementInfo? ElementAt(int x, int y) => null; // not supported via osascript

    public DumpResult? Dump(string window, int max)
    {
        // The label reported is whichever of name/title/description the app actually fills,
        // so that what --dump shows is what --name can select.
        var body = $$"""
            set ec to entire contents of win
                      set out to "{{Sentinel}}" & linefeed
                      repeat with i from 1 to count of ec
                        set el to item i of ec
                        set r to ""
                        set nm to ""
                        try
                          set r to (role of el) as string
                        end try
                        try
                          if name of el is not missing value then set nm to (name of el) as string
                        end try
                        if nm is "" then
                          try
                            if title of el is not missing value then set nm to (title of el) as string
                          end try
                        end if
                        if nm is "" then
                          try
                            if description of el is not missing value then set nm to (description of el) as string
                          end try
                        end if
                        if r is not "" then set out to out & r & tab & nm & linefeed
                      end repeat
                      return out
            """;
        var (ok, stdout, _) = Run(Wrap(window, body));
        // No sentinel means no window matched — distinct from a window with no elements,
        // which the caller reports as "not found" rather than an empty-but-successful dump.
        if (!ok || !stdout.TrimStart().StartsWith(Sentinel, StringComparison.Ordinal)) return null;

        var els = stdout.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
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
        // `value` is `missing value` on elements that have none (images, groups); coercing
        // that to text yields the literal words "missing value", so it becomes "" instead.
        var read = $"set v to \"\"\n          try\n            if value of el is not missing value then set v to (value of el) as string\n          end try\n          return \"{Sentinel}\" & v";
        var (ok, stdout, stderr) = Run(FindElementScript(window, selector, read));
        if (!ok) return UiaResult.Fail(Clean(stderr));

        var s = stdout.Trim();
        // An element whose value is genuinely empty and a window that never matched
        // both come back as empty output, so the sentinel is what separates them.
        return s.StartsWith(Sentinel, StringComparison.Ordinal)
            ? UiaResult.WithValue(s[Sentinel.Length..])
            : UiaResult.Fail($"no window matching '{window}' had a matching element (osascript)");
    }

    private UiaResult Act(string window, Selector selector, string action)
    {
        var (ok, stdout, stderr) = Run(FindElementScript(window, selector, $"{action}\n        return \"{Sentinel}\""));
        if (!ok) return UiaResult.Fail(Clean(stderr));

        // The script falls off the end of its loops (exit 0, no output) when no window
        // matched, so exit code alone would report a click that never happened. Only the
        // explicit sentinel means the action ran.
        return stdout.Trim().StartsWith(Sentinel, StringComparison.Ordinal)
            ? UiaResult.Done("ok (osascript)")
            : UiaResult.Fail($"no window matching '{window}' had a matching element (osascript)");
    }

    /// <summary>Marks output as "the script really reached the action", not an empty fall-through.</summary>
    private const string Sentinel = "@idleops-ok@";

    // Build a script that locates the window, finds the target element, then runs body.
    //
    // Three things this has to get right, all verified against System Events on macOS 26:
    //  * `entire contents` must be captured into a variable first — iterating the live
    //    specifier yields items whose every property read fails ("can't make item 1 ...
    //    into type specifier").
    //  * `name` is frequently `missing value`, and concatenating that raises, so each
    //    property is read defensively into a local.
    //  * the terser `first UI element of (entire contents of win) whose name is ...`
    //    compiles but raises at runtime: `entire contents` is a list, and a `whose`
    //    filter needs an object specifier.
    private static string FindElementScript(string window, Selector selector, string body)
    {
        var name = selector.Name ?? selector.AutomationId;
        var roles = selector.ControlType is int ct && RoleMap.TryGetValue(ControlTypes.Name(ct), out var r) ? r : null;

        var tests = new List<string>();
        // Which attribute carries a control's label varies by app: TextEdit fills `name`,
        // toolbar buttons commonly fill only `description`, and menus often only `title`.
        // Matching a --name selector against all three is what makes it usable in practice.
        if (name is not null)
        {
            var n = Escape(name);
            tests.Add($"(nm is \"{n}\" or ti is \"{n}\" or ds is \"{n}\")");
        }
        if (roles is not null) tests.Add($"({string.Join(" or ", roles.Select(x => $"rl is \"{x}\""))})");
        var condition = tests.Count > 0 ? string.Join(" and ", tests) : "true";

        var finder = $$"""
            set ec to entire contents of win
                      set el to missing value
                      repeat with i from 1 to count of ec
                        set cand to item i of ec
                        set nm to ""
                        set ti to ""
                        set ds to ""
                        set rl to ""
                        try
                          if name of cand is not missing value then set nm to (name of cand) as string
                        end try
                        try
                          if title of cand is not missing value then set ti to (title of cand) as string
                        end try
                        try
                          if description of cand is not missing value then set ds to (description of cand) as string
                        end try
                        try
                          set rl to (role of cand) as string
                        end try
                        if {{condition}} then
                          set el to cand
                          exit repeat
                        end if
                      end repeat
                      if el is missing value then error "no matching element in this window"
            """;
        return Wrap(window, $"{finder}\n          {body}");
    }

    // Both loops are guarded: System Events raises on windows exposing no `name`, and
    // -25211 ("not allowed assistive access") on whole processes such as sandboxed or
    // virtualization apps. Either aborts the enumeration unguarded, so one unrelated
    // background app would make every lookup miss.
    private static string Wrap(string window, string body)
    {
        var needle = window.Trim('*').Replace("\"", "\\\"");
        return $$"""
            tell application "System Events"
              repeat with p in (every process whose background only is false)
                try
                  repeat with win in (every window of p)
                    try
                      if name of win contains "{{needle}}" then
                        tell p
                          {{body}}
                        end tell
                      end if
                    end try
                  end repeat
                end try
              end repeat
            end tell
            return ""
            """;
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Clean(string stderr)
    {
        if (MacPermissions.IndicatesAccessibilityDenied(stderr)) return MacPermissions.AccessibilityHint;
        return string.Join(" ", stderr.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)).Trim() is { Length: > 0 } s
            ? s : "osascript: element not found or action failed";
    }

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
