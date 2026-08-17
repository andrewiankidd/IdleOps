using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;

namespace inpctl.Input;

/// <summary>
/// Linux (X11) input/window backend. Shells out to xdotool for input and window
/// control (the same external-CLI model IdleOps uses for ffmpeg), and to `kill` for
/// process interrupts. Window handles are X11 window ids. Wayland is out of scope:
/// xdotool needs X11 (or XWayland-hosted windows), and Wayland has no global window
/// addressing, so --window targeting requires an X session.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxInputBackend : IInputBackend
{
    public string Name => "xdotool (X11)";

    // Window-find is shared with scrcap + the window locator (one xdotool search).
    private readonly IdleOps.Shared.Windowing.IWindowLocator? _locator = IdleOps.Shared.Windowing.WindowLocatorFactory.Create();

    public nint FindWindow(string pattern) => _locator?.Resolve(pattern) ?? 0;

    public nint ForegroundWindow()
    {
        var (ok, stdout, _) = Run("xdotool", "getactivewindow");
        return ok && long.TryParse(stdout.Trim(), out var id) ? (nint)id : 0;
    }

    public bool Focus(nint window) =>
        Run("xdotool", "windowactivate", "--sync", Id(window)).ok;

    public WindowBounds? GetBounds(nint window)
    {
        var (ok, stdout, _) = Run("xdotool", "getwindowgeometry", "--shell", Id(window));
        if (!ok) return null;

        int x = 0, y = 0, w = 0, h = 0;
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq];
            if (!int.TryParse(line[(eq + 1)..], out var val)) continue;
            switch (key)
            {
                case "X": x = val; break;
                case "Y": y = val; break;
                case "WIDTH": w = val; break;
                case "HEIGHT": h = val; break;
            }
        }
        return new WindowBounds(x, y, w, h);
    }

    public bool MoveResize(nint window, int x, int y, int width, int height)
    {
        var moved = Run("xdotool", "windowmove", Id(window), x.ToString(), y.ToString()).ok;
        var sized = Run("xdotool", "windowsize", Id(window), width.ToString(), height.ToString()).ok;
        return moved && sized;
    }

    public bool SetState(nint window, WindowVisualState state)
    {
        switch (state)
        {
            case WindowVisualState.Minimize:
                return Run("xdotool", "windowminimize", Id(window)).ok;
            case WindowVisualState.Maximize:
                // xdotool has no maximize; wmctrl sets the EWMH maximized state.
                if (ToolExists("wmctrl"))
                    return Run("wmctrl", "-ir", Id(window), "-b", "add,maximized_vert,maximized_horz").ok;
                Console.Error.WriteLine("[inpctl] maximize needs wmctrl on PATH; skipping.");
                return true;
            case WindowVisualState.Restore:
                if (ToolExists("wmctrl"))
                    return Run("wmctrl", "-ir", Id(window), "-b", "remove,maximized_vert,maximized_horz").ok;
                return Run("xdotool", "windowactivate", Id(window)).ok;
            default:
                return true;
        }
    }

    // Linux has no "focused child control" concept to resolve; the window is the target.
    public nint ResolveInputTarget(nint window) => window;

    public bool SendKeyboard(string chord, nint target, bool background)
    {
        var specs = XdotoolKeys.Translate(chord);
        if (specs.Count == 0) return false;

        var args = new List<string> { "key", "--clearmodifiers" };
        if (background) { args.Add("--window"); args.Add(Id(target)); }
        args.AddRange(specs);
        return Run("xdotool", args.ToArray()).ok;
    }

    public bool TypeText(string text, nint target, bool background)
    {
        var args = new List<string> { "type", "--clearmodifiers" };
        if (background) { args.Add("--window"); args.Add(Id(target)); }
        args.Add("--");
        args.Add(text);
        return Run("xdotool", args.ToArray()).ok;
    }

    public bool SendMouse(string coords, nint window, MouseButton button, bool moveCursor)
    {
        if (!MouseCoords.TryParse(coords, GetBounds(window), out var points))
        {
            Console.Error.WriteLine("[inpctl] Invalid mouse coords. Expected 'x,y' or 'x1,y1-x2,y2'.");
            return false;
        }

        var btn = button switch { MouseButton.Left => "1", MouseButton.Middle => "2", MouseButton.Right => "3", _ => "1" };
        var win = Id(window);

        if (points.Count == 1)
        {
            var (x, y) = points[0];
            return Run("xdotool", "mousemove", "--window", win, x.ToString(), y.ToString()).ok
                && Run("xdotool", "click", btn).ok;
        }

        // Drag: press at the start point, move to the end point, release.
        var (sx, sy) = points[0];
        var (ex, ey) = points[1];
        return Run("xdotool", "mousemove", "--window", win, sx.ToString(), sy.ToString()).ok
            && Run("xdotool", "mousedown", btn).ok
            && Run("xdotool", "mousemove", "--window", win, ex.ToString(), ey.ToString()).ok
            && Run("xdotool", "mouseup", btn).ok;
    }

    public bool HoldForeground(string keys, double durationSeconds, CancellationToken token) =>
        Hold(keys, target: 0, background: false, durationSeconds, token);

    public bool HoldBackground(string keys, nint target, int intervalMs, double durationSeconds, CancellationToken token) =>
        // On X11 a held key auto-repeats at the server, so unlike Windows we don't
        // re-post on an interval — a single keydown/keyup pair suffices.
        Hold(keys, target, background: true, durationSeconds, token);

    private bool Hold(string keys, nint target, bool background, double durationSeconds, CancellationToken token)
    {
        var specs = XdotoolKeys.Translate(keys);
        if (specs.Count == 0) return false;

        string[] WithWindow(string verb) => background
            ? new[] { verb, "--window", Id(target) }.Concat(specs).ToArray()
            : new[] { verb }.Concat(specs).ToArray();

        if (!Run("xdotool", WithWindow("keydown")).ok) return false;
        try
        {
            var deadline = durationSeconds > 0 ? DateTime.UtcNow.AddSeconds(durationSeconds) : DateTime.MaxValue;
            while (!token.IsCancellationRequested && DateTime.UtcNow < deadline)
                Thread.Sleep(20);
        }
        finally
        {
            Run("xdotool", WithWindow("keyup"));
        }
        return true;
    }

    public bool SendInterrupt(int pid) =>
        Run("kill", "-INT", pid.ToString(CultureInfo.InvariantCulture)).ok;

    // --- helpers ---

    private static string Id(nint window) => ((long)window).ToString(CultureInfo.InvariantCulture);

    private static bool ToolExists(string tool)
    {
        try { return Run(tool, "--version").ok || Run("which", tool).ok; }
        catch { return false; }
    }

    private static (bool ok, string stdout, string stderr) Run(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p is null) return (false, "", "");
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
                Console.Error.WriteLine($"[inpctl] {file}: {stderr.Trim()}");
            return (p.ExitCode == 0, stdout, stderr);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[inpctl] failed to run {file}: {ex.Message}");
            return (false, "", "");
        }
    }
}
