using inpctl.Cli;
using inpctl.Input;

namespace inpctl;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine($"[inpctl] args: {string.Join(' ', args)}");

        var options = OptionsParser.Parse(args);

        if (options.ShowHelp || !options.HasAction)
        {
            HelpFactory.PrintHelp();
            return options.HasAction ? 0 : 1;
        }

        var backend = InputBackendFactory.Create();
        if (backend is null)
        {
            Console.Error.WriteLine("[inpctl] no input backend for this OS (supported: Windows, Linux/X11).");
            return 1;
        }

        if (options.SendCtrlC)
        {
            if (options.Pid is null)
            {
                Console.Error.WriteLine("[inpctl] --ctrlc requires --pid.");
                return 1;
            }
            return backend.SendInterrupt(options.Pid.Value) ? 0 : 1;
        }

        var hwnd = options.Window is not null
            ? backend.FindWindow(options.Window)
            : backend.ForegroundWindow();

        if (hwnd == 0)
        {
            Console.Error.WriteLine($"[inpctl] Window '{options.Window ?? "<foreground>"}' not found.");
            return 1;
        }

        if (options.Hold is not null)
        {
            return RunHold(options, hwnd, backend);
        }

        // Background input targets the window without stealing focus; foreground
        // input (default) focuses the window first.
        if (!options.Background)
        {
            if (!backend.Focus(hwnd))
                Console.Error.WriteLine("[inpctl] Could not focus target window; sending input anyway.");
        }

        var inputTarget = options.Background ? backend.ResolveInputTarget(hwnd) : hwnd;

        // Window management (before input actions)
        if (options.Maximize) { Console.WriteLine("[inpctl] Maximizing window."); backend.SetState(hwnd, WindowVisualState.Maximize); }
        else if (options.Minimize) { Console.WriteLine("[inpctl] Minimizing window."); backend.SetState(hwnd, WindowVisualState.Minimize); }
        else if (options.Restore) { Console.WriteLine("[inpctl] Restoring window."); backend.SetState(hwnd, WindowVisualState.Restore); }

        if (options.Resize is not null || options.Move is not null)
        {
            var bounds = backend.GetBounds(hwnd) ?? new WindowBounds(0, 0, 0, 0);
            var x = bounds.X; var y = bounds.Y; var w = bounds.Width; var h = bounds.Height;

            if (options.Move is not null)
            {
                var parts = options.Move.Split(',', 2);
                if (parts.Length != 2 || !int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y))
                { Console.Error.WriteLine("[inpctl] Invalid move coordinates. Expected: x,y"); return 1; }
            }

            if (options.Resize is not null)
            {
                var sep = options.Resize.Contains('x') ? 'x' : ',';
                var parts = options.Resize.Split(sep, 2);
                if (parts.Length != 2 || !int.TryParse(parts[0], out w) || !int.TryParse(parts[1], out h))
                { Console.Error.WriteLine("[inpctl] Invalid resize. Expected: WxH or W,H"); return 1; }
            }

            Console.WriteLine($"[inpctl] Moving/resizing to ({x},{y}) {w}x{h}");
            backend.MoveResize(hwnd, x, y, w, h);
        }

        // Input actions
        if (options.Keyboard is not null)
        { Console.WriteLine($"[inpctl] Sending keyboard: {options.Keyboard}{(options.Background ? " (background)" : "")}"); if (!backend.SendKeyboard(options.Keyboard, inputTarget, options.Background)) return 1; }

        if (options.Type is not null)
        { Console.WriteLine($"[inpctl] Typing text: {options.Type}{(options.Background ? " (background)" : "")}"); if (!backend.TypeText(options.Type, inputTarget, options.Background)) return 1; }

        if (options.LeftMouse is not null)
        { Console.WriteLine($"[inpctl] Left mouse: {options.LeftMouse}"); if (!backend.SendMouse(options.LeftMouse, hwnd, MouseButton.Left, options.MoveCursor)) return 1; }

        if (options.RightMouse is not null)
        { Console.WriteLine($"[inpctl] Right mouse: {options.RightMouse}"); if (!backend.SendMouse(options.RightMouse, hwnd, MouseButton.Right, options.MoveCursor)) return 1; }

        if (options.MiddleMouse is not null)
        { Console.WriteLine($"[inpctl] Middle mouse: {options.MiddleMouse}"); if (!backend.SendMouse(options.MiddleMouse, hwnd, MouseButton.Middle, options.MoveCursor)) return 1; }

        return 0;
    }

    // Hold key(s) down until the duration elapses or Ctrl+C. Foreground focuses the
    // window first; background targets the window without stealing focus.
    private static int RunHold(Options options, nint hwnd, IInputBackend backend)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var mode = options.Method == InputMethod.Background ? "background" : "foreground";
        if (options.Duration <= 0)
            Console.Error.WriteLine($"[inpctl] Holding '{options.Hold}' ({mode}) — press Ctrl+C to release.");
        else
            Console.Error.WriteLine($"[inpctl] Holding '{options.Hold}' ({mode}) for {options.Duration:0.#}s...");

        if (options.Method == InputMethod.Background)
        {
            var target = backend.ResolveInputTarget(hwnd);
            return backend.HoldBackground(options.Hold!, target, options.Interval, options.Duration, cts.Token) ? 0 : 1;
        }

        if (!backend.Focus(hwnd))
            Console.Error.WriteLine("[inpctl] Could not focus target window; holding anyway.");
        return backend.HoldForeground(options.Hold!, options.Duration, cts.Token) ? 0 : 1;
    }
}
