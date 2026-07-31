using System.Diagnostics;
using System.Runtime.InteropServices;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windows;
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

        if (!PlatformSupport.EnsureWindows("inpctl")) return 1;

        if (options.SendCtrlC)
        {
            if (options.Pid is null)
            {
                Console.Error.WriteLine("[inpctl] --ctrlc requires --pid.");
                return 1;
            }
            return SendCtrlC(options.Pid.Value) ? 0 : 1;
        }

        var hwnd = options.Window is not null
            ? WindowMatcher.FindWindow(options.Window, preferNewest: true)?.Handle ?? IntPtr.Zero
            : GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
        {
            Console.Error.WriteLine($"[inpctl] Window '{options.Window ?? "<foreground>"}' not found.");
            return 1;
        }

        // Background input posts messages to the target without stealing focus.
        // Foreground input (default) uses SendInput, which requires the window up front.
        if (!options.Background)
        {
            if (!FocusWindow(hwnd))
            {
                Console.Error.WriteLine("[inpctl] Could not focus target window; sending input anyway.");
            }
        }

        // SendInput ignores hwnd (it hits the focused control); PostMessage needs the
        // focused child window of the target's thread, not the top-level frame.
        var inputTarget = options.Background ? ResolveInputTarget(hwnd) : hwnd;

        // Window management (before input actions)
        if (options.Maximize) { Console.WriteLine("[inpctl] Maximizing window."); ShowWindow(hwnd, 3); }
        else if (options.Minimize) { Console.WriteLine("[inpctl] Minimizing window."); ShowWindow(hwnd, 6); }
        else if (options.Restore) { Console.WriteLine("[inpctl] Restoring window."); ShowWindow(hwnd, 9); }

        if (options.Resize is not null || options.Move is not null)
        {
            var rect = WindowMatcher.GetWindowBounds(hwnd);
            var x = rect.Left; var y = rect.Top; var w = rect.Width; var h = rect.Height;

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
            MoveWindow(hwnd, x, y, w, h, true);
        }

        // Input actions
        if (options.Keyboard is not null)
        { Console.WriteLine($"[inpctl] Sending keyboard: {options.Keyboard}{(options.Background ? " (background)" : "")}"); if (!InputSender.SendKeyboard(options.Keyboard, inputTarget, options.Background)) return 1; }

        if (options.Type is not null)
        { Console.WriteLine($"[inpctl] Typing text: {options.Type}{(options.Background ? " (background)" : "")}"); if (!InputSender.TypeText(options.Type, inputTarget, options.Background)) return 1; }

        if (options.LeftMouse is not null)
        { Console.WriteLine($"[inpctl] Left mouse: {options.LeftMouse}"); if (!InputSender.SendMouse(options.LeftMouse, hwnd, MouseButton.Left, options.MoveCursor)) return 1; }

        if (options.RightMouse is not null)
        { Console.WriteLine($"[inpctl] Right mouse: {options.RightMouse}"); if (!InputSender.SendMouse(options.RightMouse, hwnd, MouseButton.Right, options.MoveCursor)) return 1; }

        if (options.MiddleMouse is not null)
        { Console.WriteLine($"[inpctl] Middle mouse: {options.MiddleMouse}"); if (!InputSender.SendMouse(options.MiddleMouse, hwnd, MouseButton.Middle, options.MoveCursor)) return 1; }

        return 0;
    }

    private static bool SendCtrlC(int pid)
    {
        try { _ = Process.GetProcessById(pid); }
        catch { Console.Error.WriteLine($"[inpctl] Process {pid} not found."); return false; }

        FreeConsole();

        if (!AttachConsole((uint)pid))
        { Console.Error.WriteLine($"[inpctl] Could not attach to console for PID {pid}."); return false; }

        try
        {
            SetConsoleCtrlHandler(null, true);
            if (!GenerateConsoleCtrlEvent(0, 0))
            { Console.Error.WriteLine($"[inpctl] Failed to send CTRL+C to PID {pid}."); return false; }

            try
            {
                using var target = Process.GetProcessById(pid);
                if (!target.WaitForExit(5000))
                { Console.Error.WriteLine("[inpctl] Target did not exit."); return false; }
            }
            catch { /* best-effort */ }
            return true;
        }
        finally { SetConsoleCtrlHandler(null, false); FreeConsole(); }
    }

    private static bool FocusWindow(IntPtr hwnd)
    {
        AllowSetForegroundWindow(0xFFFFFFFF);
        for (var i = 0; i < 10; i++)
        {
            var windowThread = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
            var currentThread = GetCurrentThreadId();
            if (windowThread != 0 && currentThread != 0 && windowThread != currentThread)
                AttachThreadInput(currentThread, windowThread, true);

            ShowWindow(hwnd, 9);
            SwitchToThisWindow(hwnd, true);
            BringWindowToTop(hwnd);
            if (SetForegroundWindow(hwnd)) return true;
            Thread.Sleep(150);
        }
        return false;
    }

    // For background posting, find the window that actually holds keyboard focus
    // within the target's thread (the edit control / render widget), not the frame.
    // Falls back to the top-level window when the thread has no focused child
    // (common for an inactive window — background input is best-effort there).
    private static IntPtr ResolveInputTarget(IntPtr hwnd)
    {
        var threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        if (threadId == 0) return hwnd;

        var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
        if (GetGUIThreadInfo(threadId, ref info) && info.hwndFocus != IntPtr.Zero)
        {
            return info.hwndFocus;
        }
        return hwnd;
    }

    // P/Invoke — window management and process control
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(uint dwProcessId);
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();
    [DllImport("kernel32.dll")] private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    [DllImport("kernel32.dll")] private static extern bool SetConsoleCtrlHandler(Delegate? handlerRoutine, bool add);
}
