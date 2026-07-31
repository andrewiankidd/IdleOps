using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using IdleOps.Shared.Windows;
using IdleOps.Shared.Windows.Uia;

namespace stpcap.Recording;

internal sealed class InputRecorder : IDisposable
{
    private readonly string? _windowFilter;
    private readonly Regex? _windowRegex;
    private readonly List<InputEvent> _events = [];
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private readonly LowLevelKeyboardProc _keyboardCallback;
    private readonly LowLevelMouseProc _mouseCallback;

    // UI Automation, used to capture the element under the cursor at click time
    // so we can emit resilient semantic steps. Best-effort: null if UIA is
    // unavailable, and every query is wrapped so recording never breaks.
    private readonly UiaAutomation? _uia;

    // Track typed characters for coalescing into --type strings
    private readonly StringBuilder _typingBuffer = new();
    private string? _typingWindow;
    private DateTime _lastTypingTime;

    // Track mouse drag state
    private bool _mouseDown;
    private int _mouseDownX, _mouseDownY;
    private DateTime _mouseDownTime;
    private string? _mouseDownWindow;
    private ElementInfo? _mouseDownElement;

    public InputRecorder(string? windowFilter)
    {
        _windowFilter = windowFilter;
        _windowRegex = windowFilter is not null ? WindowMatcher.BuildWildcardRegex(windowFilter) : null;
        _keyboardCallback = KeyboardHookCallback;
        _mouseCallback = MouseHookCallback;
        try { _uia = new UiaAutomation(); } catch { _uia = null; }
    }

    // Query the element under a screen point at click time (before the app reacts).
    private ElementInfo? ElementAt(int x, int y)
    {
        try { return _uia?.ElementAt(x, y); }
        catch { return null; }
    }

    public IReadOnlyList<InputEvent> Events => _events;

    public void Start()
    {
        _keyboardHook = SetHook(WH_KEYBOARD_LL, _keyboardCallback);
        _mouseHook = SetHook(WH_MOUSE_LL, _mouseCallback);
    }

    public void Stop()
    {
        FlushTypingBuffer();
        if (_keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
        if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
    }

    public void Dispose() => Stop();

    private string? GetActiveWindowTitle()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return null;
        return WindowMatcher.GetWindowTitle(hwnd);
    }

    private bool IsTargetWindow(string? title)
    {
        if (_windowRegex is null) return true;
        if (title is null) return false;
        return _windowRegex.IsMatch(title);
    }

    private void FlushTypingBuffer()
    {
        if (_typingBuffer.Length > 0)
        {
            _events.Add(new InputEvent(
                InputEventType.TextInput,
                _lastTypingTime,
                _typingWindow)
            {
                Character = '\0' // not used for TextInput; the text is in the typing buffer
            });
            // Store the text in a way ScriptGenerator can access
            // We'll use a simple approach: store as a special event
            _events[^1] = _events[^1] with { Button = _typingBuffer.ToString() }; // repurpose Button field for text
            _typingBuffer.Clear();
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = (ushort)Marshal.ReadInt32(lParam);
            var msg = (int)wParam;

            var title = GetActiveWindowTitle();
            if (IsTargetWindow(title))
            {
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    // Check if this is a printable character (for typing coalescing)
                    var ch = VkToChar(vkCode);
                    if (ch != '\0' && !IsModifierPressed())
                    {
                        if (_typingBuffer.Length > 0 && title != _typingWindow)
                        {
                            FlushTypingBuffer();
                        }
                        _typingBuffer.Append(ch);
                        _typingWindow = title;
                        _lastTypingTime = DateTime.UtcNow;
                    }
                    else
                    {
                        FlushTypingBuffer();
                        _events.Add(new InputEvent(InputEventType.KeyDown, DateTime.UtcNow, title, VirtualKey: vkCode));
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    // Only record key-up for modifier/special keys (not typed characters)
                    if (IsModifierKey(vkCode))
                    {
                        _events.Add(new InputEvent(InputEventType.KeyUp, DateTime.UtcNow, title, VirtualKey: vkCode));
                    }
                }
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var msg = (int)wParam;
            var title = GetActiveWindowTitle();

            if (IsTargetWindow(title))
            {
                FlushTypingBuffer();

                if (msg == WM_LBUTTONDOWN)
                {
                    _mouseDown = true;
                    _mouseDownX = hookStruct.pt.x;
                    _mouseDownY = hookStruct.pt.y;
                    _mouseDownTime = DateTime.UtcNow;
                    _mouseDownWindow = title;
                    // Capture the element now, before the click is processed and the UI changes.
                    _mouseDownElement = ElementAt(hookStruct.pt.x, hookStruct.pt.y);
                }
                else if (msg == WM_LBUTTONUP && _mouseDown)
                {
                    _mouseDown = false;
                    var dx = Math.Abs(hookStruct.pt.x - _mouseDownX);
                    var dy = Math.Abs(hookStruct.pt.y - _mouseDownY);

                    if (dx > 5 || dy > 5)
                    {
                        _events.Add(new InputEvent(InputEventType.MouseDrag, _mouseDownTime, _mouseDownWindow,
                            X: _mouseDownX, Y: _mouseDownY, EndX: hookStruct.pt.x, EndY: hookStruct.pt.y, Button: "left"));
                    }
                    else
                    {
                        _events.Add(new InputEvent(InputEventType.MouseClick, _mouseDownTime, _mouseDownWindow,
                            X: _mouseDownX, Y: _mouseDownY, Button: "left", Element: _mouseDownElement));
                    }
                    _mouseDownElement = null;
                }
                else if (msg == WM_RBUTTONDOWN)
                {
                    _events.Add(new InputEvent(InputEventType.MouseClick, DateTime.UtcNow, title,
                        X: hookStruct.pt.x, Y: hookStruct.pt.y, Button: "right"));
                }
                else if (msg == WM_MBUTTONDOWN)
                {
                    _events.Add(new InputEvent(InputEventType.MouseClick, DateTime.UtcNow, title,
                        X: hookStruct.pt.x, Y: hookStruct.pt.y, Button: "middle"));
                }
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private static char VkToChar(ushort vk)
    {
        var keyboardState = new byte[256];
        GetKeyboardState(keyboardState);
        var sb = new StringBuilder(2);
        var result = ToUnicode(vk, 0, keyboardState, sb, sb.Capacity, 0);
        return result == 1 ? sb[0] : '\0';
    }

    private static bool IsModifierPressed()
    {
        return (GetAsyncKeyState(0x10) & 0x8000) != 0  // SHIFT (not counted as modifier for typing)
            ? false  // shift is fine for typing
            : (GetAsyncKeyState(0x11) & 0x8000) != 0   // CTRL
              || (GetAsyncKeyState(0x12) & 0x8000) != 0; // ALT
    }

    private static bool IsModifierKey(ushort vk) => vk is 0x10 or 0x11 or 0x12; // SHIFT, CTRL, ALT

    // --- P/Invoke ---

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private static IntPtr SetHook(int hookType, Delegate callback)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return SetWindowsHookEx(hookType, callback, GetModuleHandle(module.ModuleName), 0);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, StringBuilder pwszBuff, int cchBuff, uint wFlags);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
