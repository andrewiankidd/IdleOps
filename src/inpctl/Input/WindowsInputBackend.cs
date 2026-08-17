using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using IdleOps.Shared.Windows;

namespace inpctl.Input;

/// <summary>
/// Windows input/window backend: user32 SendInput/PostMessage for input, plus the
/// window-management and console-control P/Invokes previously inlined in Program.cs.
/// Keyboard/mouse logic delegates to <see cref="InputSender"/> (unchanged).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsInputBackend : IInputBackend
{
    public string Name => "windows";

    public nint FindWindow(string pattern) =>
        WindowMatcher.FindWindow(pattern, preferNewest: true)?.Handle ?? IntPtr.Zero;

    public nint ForegroundWindow() => GetForegroundWindow();

    public WindowBounds? GetBounds(nint window)
    {
        var rect = WindowMatcher.GetWindowBounds(window);
        return new WindowBounds(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public bool MoveResize(nint window, int x, int y, int width, int height) =>
        MoveWindow(window, x, y, width, height, true);

    public bool SetState(nint window, WindowVisualState state) => ShowWindow(window, state switch
    {
        WindowVisualState.Maximize => 3,
        WindowVisualState.Minimize => 6,
        WindowVisualState.Restore => 9,
        _ => 9,
    });

    // Bring the window forward, working around Windows' foreground-lock via the
    // AttachThreadInput dance (retried a few times for stubborn windows).
    public bool Focus(nint hwnd)
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
    public nint ResolveInputTarget(nint hwnd)
    {
        var threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        if (threadId == 0) return hwnd;

        var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
        if (GetGUIThreadInfo(threadId, ref info) && info.hwndFocus != IntPtr.Zero)
            return info.hwndFocus;
        return hwnd;
    }

    public bool SendKeyboard(string chord, nint target, bool background) =>
        InputSender.SendKeyboard(chord, target, background);

    public bool TypeText(string text, nint target, bool background) =>
        InputSender.TypeText(text, target, background);

    public bool SendMouse(string coords, nint window, MouseButton button, bool moveCursor) =>
        InputSender.SendMouse(coords, window, button, moveCursor);

    public bool HoldForeground(string keys, double durationSeconds, CancellationToken token) =>
        InputSender.HoldForeground(keys, durationSeconds, token);

    public bool HoldBackground(string keys, nint target, int intervalMs, double durationSeconds, CancellationToken token) =>
        InputSender.HoldBackground(keys, target, intervalMs, durationSeconds, token);

    // Send a real console CTRL+C to another process by attaching to its console.
    public bool SendInterrupt(int pid)
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

    // --- P/Invoke — window management and process control ---
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
