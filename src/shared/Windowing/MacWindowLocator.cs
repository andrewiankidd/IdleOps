using System.Globalization;
using System.Runtime.Versioning;
using IdleOps.Shared.Capture;

namespace IdleOps.Shared.Windowing;

/// <summary>
/// macOS window presence + bounds via AppleScript System Events (osascript). macOS
/// has no simple global window id exposed to scripting, so <see cref="Resolve"/>
/// returns 0 — callers target windows by their screen bounds instead.
///
/// UNVERIFIED: written without a Mac to test on. Needs Accessibility permission
/// granted to the terminal/host in System Settings › Privacy &amp; Security.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacWindowLocator : IWindowLocator
{
    public string Name => "osascript (System Events)";

    public bool Exists(string pattern) => Bounds(pattern) is not null;

    // macOS scripting has no stable window id; bounds-based targeting is used instead.
    public nint Resolve(string pattern) => 0;

    public string? ResolveTitle(string pattern) => QueryWindow(pattern, wantTitle: true);

    public WindowBounds? GetBounds(string pattern) => Bounds(pattern);

    private static WindowBounds? Bounds(string pattern)
    {
        var raw = QueryWindow(pattern, wantTitle: false);
        if (raw is null) return null;
        var p = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (p.Length != 4) return null;
        return int.TryParse(p[0], out var x) && int.TryParse(p[1], out var y)
            && int.TryParse(p[2], out var w) && int.TryParse(p[3], out var h)
            ? new WindowBounds(x, y, w, h) : null;
    }

    // Find the first window whose title contains the pattern (wildcards stripped) and
    // return either its "x,y,w,h" bounds or its title.
    private static string? QueryWindow(string pattern, bool wantTitle)
    {
        var needle = pattern.Trim('*').Replace("\"", "\\\"");
        var result = wantTitle ? "name of w" : "((item 1 of pos) & \",\" & (item 2 of pos) & \",\" & (item 1 of sz) & \",\" & (item 2 of sz))";
        var script = $$"""
            tell application "System Events"
              repeat with p in (every process whose background only is false)
                repeat with w in (every window of p)
                  if name of w contains "{{needle}}" then
                    set pos to position of w
                    set sz to size of w
                    return {{result}} as string
                  end if
                end repeat
              end repeat
            end tell
            return ""
            """;
        var (ok, stdout, _) = ProcessRunner.Run("osascript", "-e", script);
        if (!ok) return null;
        var s = stdout.Trim();
        return s.Length == 0 ? null : s;
    }
}
