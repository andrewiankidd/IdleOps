namespace inpctl.Input;

/// <summary>Window rectangle in screen pixels.</summary>
internal readonly record struct WindowBounds(int X, int Y, int Width, int Height);

/// <summary>Show/size state for a window.</summary>
internal enum WindowVisualState { Maximize, Minimize, Restore }

/// <summary>Which mouse button an action uses.</summary>
internal enum MouseButton { Left, Right, Middle }

/// <summary>
/// A platform's input + window-control backend. Windows uses user32
/// (SendInput/PostMessage); Linux (X11) shells out to xdotool. Window handles are
/// opaque <see cref="nint"/> values — an HWND on Windows, an X11 window id on
/// Linux — where 0 means "no window".
///
/// Selected by <see cref="InputBackendFactory"/> so Program.cs stays
/// platform-agnostic (mirrors audcap's IAudioCapturer factory).
/// </summary>
internal interface IInputBackend
{
    /// <summary>Human label for logs/errors, e.g. "windows" or "xdotool (X11)".</summary>
    string Name { get; }

    // --- Window targeting & management ---

    /// <summary>Resolve a title pattern (supports * wildcards) to a window handle, or 0 if none.</summary>
    nint FindWindow(string pattern);

    /// <summary>The currently focused/active window, or 0.</summary>
    nint ForegroundWindow();

    /// <summary>Bring the window to the foreground / give it focus.</summary>
    bool Focus(nint window);

    /// <summary>Screen-pixel bounds of the window, or null if unavailable.</summary>
    WindowBounds? GetBounds(nint window);

    /// <summary>Move and resize the window in one operation (screen pixels).</summary>
    bool MoveResize(nint window, int x, int y, int width, int height);

    /// <summary>Maximize / minimize / restore the window.</summary>
    bool SetState(nint window, WindowVisualState state);

    /// <summary>
    /// The handle background (no-focus) input should target. On Windows this is the
    /// focused child control of the window's thread; elsewhere it is the window itself.
    /// </summary>
    nint ResolveInputTarget(nint window);

    // --- Input ---

    /// <summary>Send a key chord/sequence (e.g. "CTRL+S", "CTRL+A, DELETE").</summary>
    bool SendKeyboard(string chord, nint target, bool background);

    /// <summary>Type literal text.</summary>
    bool TypeText(string text, nint target, bool background);

    /// <summary>Click (or drag) at window-relative or percentage coordinates.</summary>
    bool SendMouse(string coords, nint window, MouseButton button, bool moveCursor);

    /// <summary>Hold key(s) with the window focused until the duration elapses or cancellation.</summary>
    bool HoldForeground(string keys, double durationSeconds, CancellationToken token);

    /// <summary>Hold key(s) by re-posting to the target window without stealing focus.</summary>
    bool HoldBackground(string keys, nint target, int intervalMs, double durationSeconds, CancellationToken token);

    // --- Process control ---

    /// <summary>Interrupt a process by PID (Ctrl+C on Windows, SIGINT on Linux).</summary>
    bool SendInterrupt(int pid);
}
