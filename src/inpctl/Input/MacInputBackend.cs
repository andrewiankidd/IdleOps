using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;

namespace inpctl.Input;

/// <summary>
/// macOS input/window backend. Uses `cliclick` (Homebrew: `brew install cliclick`)
/// for mouse + keyboard, and `osascript`/System Events for window focus, move and
/// resize; window bounds come from the shared osascript window locator. cliclick is
/// screen-coordinate based, so window-relative clicks are offset by the target
/// window's origin.
///
/// UNVERIFIED: written without a Mac to test on. Needs `cliclick` on PATH and
/// Accessibility permission (System Settings › Privacy &amp; Security › Accessibility)
/// for the terminal/host, else input and window scripting are blocked.
///
/// macOS scripting has no stable window id, so FindWindow returns a sentinel (1 =
/// found) and caches the pattern/bounds for the rest of this one-shot run — inpctl
/// targets a single window per invocation.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacInputBackend : IInputBackend
{
    public string Name => "cliclick + osascript (macOS)";

    private readonly IdleOps.Shared.Windowing.IWindowLocator? _locator = IdleOps.Shared.Windowing.WindowLocatorFactory.Create();
    private string? _pattern;
    private (int x, int y, int w, int h)? _bounds;

    public nint FindWindow(string pattern)
    {
        _pattern = pattern;
        _bounds = _locator?.GetBounds(pattern) is { } b ? (b.X, b.Y, b.Width, b.Height) : null;
        return _bounds is null ? 0 : 1;
    }

    public nint ForegroundWindow() => 1; // input targets the frontmost app

    public WindowBounds? GetBounds(nint window) =>
        _bounds is { } b ? new WindowBounds(b.x, b.y, b.w, b.h) : null;

    public nint ResolveInputTarget(nint window) => window; // no per-control targeting on macOS

    public bool Focus(nint window) => _pattern is not null && WindowScript(_pattern,
        "set frontmost of p to true\n    try\n      perform action \"AXRaise\" of w\n    end try");

    public bool MoveResize(nint window, int x, int y, int width, int height) => _pattern is not null && WindowScript(_pattern,
        $"set position of w to {{{x}, {y}}}\n    set size of w to {{{width}, {height}}}");

    public bool SetState(nint window, WindowVisualState state) => _pattern is not null && WindowScript(_pattern, state switch
    {
        WindowVisualState.Minimize => "set value of attribute \"AXMinimized\" of w to true",
        WindowVisualState.Restore => "set value of attribute \"AXMinimized\" of w to false",
        WindowVisualState.Maximize => "try\n      set value of attribute \"AXFullScreen\" of w to true\n    end try",
        _ => "",
    });

    public bool SendKeyboard(string chord, nint target, bool background)
    {
        var args = MacKeys.Translate(chord);
        return args.Count > 0 && Run("cliclick", args.ToArray()).ok;
    }

    public bool TypeText(string text, nint target, bool background) => Run("cliclick", $"t:{text}").ok;

    public bool SendMouse(string coords, nint window, MouseButton button, bool moveCursor)
    {
        var lb = _bounds is { } b ? new WindowBounds(b.x, b.y, b.w, b.h) : (WindowBounds?)null;
        if (!MouseCoords.TryParse(coords, lb, out var pts))
        {
            Console.Error.WriteLine("[inpctl] Invalid mouse coords. Expected 'x,y' or 'x1,y1-x2,y2'.");
            return false;
        }
        int ox = _bounds?.x ?? 0, oy = _bounds?.y ?? 0;
        var click = button switch { MouseButton.Right => "rc", MouseButton.Middle => "tc", _ => "c" };

        if (pts.Count == 1)
        {
            var (x, y) = pts[0];
            return Run("cliclick", $"{click}:{ox + x},{oy + y}").ok;
        }
        var (sx, sy) = pts[0];
        var (ex, ey) = pts[1];
        return Run("cliclick", $"dd:{ox + sx},{oy + sy}", $"du:{ox + ex},{oy + ey}").ok;
    }

    // cliclick holds a key down across separate invocations (CGEvent keydown persists),
    // so keydown, wait, keyup mirrors the Linux/Windows hold. Simple keys only.
    public bool HoldForeground(string keys, double durationSeconds, CancellationToken token) => Hold(keys, durationSeconds, token);
    public bool HoldBackground(string keys, nint target, int intervalMs, double durationSeconds, CancellationToken token) => Hold(keys, durationSeconds, token);

    private static bool Hold(string keys, double durationSeconds, CancellationToken token)
    {
        var spec = MacKeys.Translate(keys).FirstOrDefault(s => s.StartsWith("kp:") || s.StartsWith("t:"));
        if (spec is null) return false;
        var name = spec.Split(':', 2)[1];
        if (!Run("cliclick", $"kd:{name}").ok) return false;
        try
        {
            var deadline = durationSeconds > 0 ? DateTime.UtcNow.AddSeconds(durationSeconds) : DateTime.MaxValue;
            while (!token.IsCancellationRequested && DateTime.UtcNow < deadline) Thread.Sleep(20);
        }
        finally { Run("cliclick", $"ku:{name}"); }
        return true;
    }

    public bool SendInterrupt(int pid) => Run("kill", "-INT", pid.ToString(CultureInfo.InvariantCulture)).ok;

    // Run an AppleScript body against the first window whose title contains the pattern.
    private static bool WindowScript(string pattern, string body)
    {
        if (string.IsNullOrEmpty(body)) return true;
        var needle = pattern.Trim('*').Replace("\"", "\\\"");
        var script = $$"""
            tell application "System Events"
              repeat with p in (every process whose background only is false)
                repeat with w in (every window of p)
                  if name of w contains "{{needle}}" then
                    {{body}}
                    return "ok"
                  end if
                end repeat
              end repeat
            end tell
            """;
        return Run("osascript", "-e", script).ok;
    }

    private static (bool ok, string stdout, string stderr) Run(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = file, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p is null) return (false, "", "");
            var so = p.StandardOutput.ReadToEnd();
            var se = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0 && !string.IsNullOrWhiteSpace(se)) Console.Error.WriteLine($"[inpctl] {file}: {se.Trim()}");
            return (p.ExitCode == 0, so, se);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[inpctl] failed to run {file}: {ex.Message} (is it installed? `brew install cliclick`)");
            return (false, "", "");
        }
    }
}
