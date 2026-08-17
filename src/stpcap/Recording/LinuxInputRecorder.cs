using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using IdleOps.Shared.Windows;
using static stpcap.Recording.XRecordInterop;

namespace stpcap.Recording;

/// <summary>
/// Linux backend: records X input through the RECORD extension via P/Invoke (libXtst),
/// coalescing printable keys into typed-text steps and mapping special keys onto the VK
/// names ScriptGenerator understands. No AT-SPI element capture, so clicks record as
/// window-relative coordinates.
///
/// XRecord needs two X connections: the control one creates and later disables the
/// context, while the data one blocks inside XRecordEnableContext delivering events. They
/// cannot be the same connection — enabling the context takes the data connection over
/// entirely, so a single connection would have no way to ever stop itself.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxInputRecorder : IInputRecorder
{
    private readonly Regex? _filter;
    private readonly List<InputEvent> _events = [];
    private readonly StringBuilder _typing = new();
    private string? _typingWindow;

    private IntPtr _control;      // creates + disables the context, and answers title queries
    private IntPtr _data;         // blocks in XRecordEnableContext
    private IntPtr _context;
    private XRecordInterceptProc? _callback;   // must outlive the native call: GC would collect it
    private bool _shiftDown;

    // X keysym name -> Windows VK, so ScriptGenerator.VkToName emits inpctl key names.
    private static readonly Dictionary<string, ushort> SpecialVk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Return"] = 0x0D, ["KP_Enter"] = 0x0D, ["Tab"] = 0x09, ["BackSpace"] = 0x08,
        ["Escape"] = 0x1B, ["Delete"] = 0x2E, ["Home"] = 0x24, ["End"] = 0x23,
        ["Left"] = 0x25, ["Up"] = 0x26, ["Right"] = 0x27, ["Down"] = 0x28,
        ["Prior"] = 0x21, ["Next"] = 0x22,
        ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73, ["F5"] = 0x74, ["F6"] = 0x75,
        ["F7"] = 0x76, ["F8"] = 0x77, ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B,
    };

    // Keysyms whose press should never be recorded as input of its own.
    private static readonly HashSet<string> ModifierKeysyms = new(StringComparer.Ordinal)
    {
        "Shift_L", "Shift_R", "Control_L", "Control_R", "Alt_L", "Alt_R",
        "Super_L", "Super_R", "Meta_L", "Meta_R", "Caps_Lock", "ISO_Level3_Shift", "Num_Lock",
    };

    public LinuxInputRecorder(string? windowFilter) =>
        _filter = windowFilter is not null ? WindowMatcher.BuildWildcardRegex(windowFilter) : null;

    public string Name => "xrecord (X11)";
    public IReadOnlyList<InputEvent> Events => _events;

    public void RunUntil(CancellationToken token)
    {
        if (!TryOpen()) return;

        // XRecordEnableContext blocks for the lifetime of the recording, so it runs on its
        // own thread and cancellation disables the context from the control connection.
        var pump = new Thread(() =>
        {
            try { XRecordEnableContext(_data, _context, _callback!, IntPtr.Zero); }
            catch (Exception ex) { Console.Error.WriteLine($"[stpcap] XRecord pump stopped: {ex.Message}"); }
        })
        { IsBackground = true };
        pump.Start();

        try { while (!token.IsCancellationRequested) Thread.Sleep(50); }
        finally
        {
            try { XRecordDisableContext(_control, _context); XFlush(_control); } catch { /* shutting down */ }
            pump.Join(2000);
            FlushTyping();
        }
    }

    private bool TryOpen()
    {
        try
        {
            _control = XOpenDisplay(null);
            _data = XOpenDisplay(null);
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine($"[stpcap] X11 libraries not found ({ex.Message}). Recording needs libX11 and libXtst (apt install libx11-6 libxtst6).");
            return false;
        }

        if (_control == IntPtr.Zero || _data == IntPtr.Zero)
        {
            Console.Error.WriteLine("[stpcap] cannot open the X display. Is DISPLAY set and an X server running?");
            return false;
        }

        var range = XRecordAllocRange();
        if (range == IntPtr.Zero) { Console.Error.WriteLine("[stpcap] XRecordAllocRange failed."); return false; }

        // KeyPress..ButtonPress also delivers KeyRelease, which is what tracks shift.
        var r = Marshal.PtrToStructure<XRecordRange>(range);
        r.DeviceEvents.First = KeyPress;
        r.DeviceEvents.Last = ButtonPress;
        Marshal.StructureToPtr(r, range, false);

        var clients = new[] { AllClients };
        var ranges = new[] { range };
        _context = XRecordCreateContext(_control, 0, clients, 1, ranges, 1);
        XFree(range);

        if (_context == IntPtr.Zero)
        {
            Console.Error.WriteLine("[stpcap] XRecordCreateContext failed — does this X server have the RECORD extension?");
            return false;
        }

        _callback = OnIntercept;
        XFlush(_control);
        return true;
    }

    // Called on the pump thread for each batch the server sends.
    private void OnIntercept(IntPtr closure, IntPtr recordedData)
    {
        try
        {
            if (recordedData == IntPtr.Zero) return;
            var intercept = Marshal.PtrToStructure<XRecordInterceptData>(recordedData);
            if (intercept.Category != FromServer || intercept.Data == IntPtr.Zero) return;

            // DataLen counts 4-byte units; each core event is 32 bytes.
            var bytes = intercept.DataLen * 4;
            for (var offset = 0; offset + EventSize <= bytes; offset += EventSize)
            {
                HandleEvent(intercept.Data + offset);
            }
        }
        catch (Exception ex) { Console.Error.WriteLine($"[stpcap] dropped an event: {ex.Message}"); }
        finally { XRecordFreeData(recordedData); }
    }

    // Core X event wire format: [0]=type, [1]=detail, root_x at 20, root_y at 22.
    private void HandleEvent(IntPtr ev)
    {
        var type = Marshal.ReadByte(ev, 0);
        var detail = Marshal.ReadByte(ev, 1);
        var rootX = Marshal.ReadInt16(ev, 20);
        var rootY = Marshal.ReadInt16(ev, 22);

        switch (type)
        {
            case KeyPress:
            {
                var baseName = KeysymName(detail, shifted: false);
                if (baseName is "Shift_L" or "Shift_R") { _shiftDown = true; return; }
                if (baseName is not null && ModifierKeysyms.Contains(baseName)) return;

                var text = KeysymText(detail, _shiftDown);
                var window = ActiveWindowTitle();
                if (!Matches(window)) return;

                if (text is not null)
                {
                    if (_typing.Length > 0 && _typingWindow != window) FlushTyping();
                    _typingWindow = window;
                    _typing.Append(text);
                }
                else if (baseName is not null && SpecialVk.TryGetValue(baseName, out var vk))
                {
                    FlushTyping();
                    Add(new InputEvent(InputEventType.KeyDown, DateTime.UtcNow, window, VirtualKey: vk));
                }
                return;
            }

            case KeyRelease:
                if (KeysymName(detail, shifted: false) is "Shift_L" or "Shift_R") _shiftDown = false;
                return;

            case ButtonPress:
            {
                var window = ActiveWindowTitle();
                if (!Matches(window)) return;
                FlushTyping();
                var button = detail == 3 ? "right" : detail == 2 ? "middle" : "left";
                Add(new InputEvent(InputEventType.MouseClick, DateTime.UtcNow, window, X: rootX, Y: rootY, Button: button));
                return;
            }
        }
    }

    private bool Matches(string? window) =>
        _filter is null || (window is not null && _filter.IsMatch(window));

    private string? KeysymName(byte keycode, bool shifted)
    {
        var ks = XkbKeycodeToKeysym(_control, keycode, 0, shifted ? 1 : 0);
        if (ks == UIntPtr.Zero) return null;
        var p = XKeysymToString(ks);
        return p == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(p);
    }

    /// <summary>The printable text a keycode produces, or null when it is not a text key.</summary>
    private string? KeysymText(byte keycode, bool shifted)
    {
        var ks = XkbKeycodeToKeysym(_control, keycode, 0, shifted ? 1 : 0);
        if (ks == UIntPtr.Zero && shifted) ks = XkbKeycodeToKeysym(_control, keycode, 0, 0);
        var value = (ulong)ks;
        if (value == 0) return null;

        // Latin-1 keysyms are their own codepoint; 0x01xxxxxx keysyms carry Unicode directly.
        if (value is >= 0x20 and <= 0x7e || value is >= 0xa0 and <= 0xff) return ((char)value).ToString();
        if ((value & 0xff000000) == 0x01000000)
        {
            var cp = (int)(value & 0x00ffffff);
            try { return char.ConvertFromUtf32(cp); } catch { return null; }
        }
        return null;
    }

    /// <summary>Title of the focused top-level window, matching what the tools target by.</summary>
    private string? ActiveWindowTitle()
    {
        try
        {
            var root = XDefaultRootWindow(_control);
            var active = XInternAtom(_control, "_NET_ACTIVE_WINDOW", true);
            if (active != IntPtr.Zero && ReadWindowProperty(root, active) is { } w && w != IntPtr.Zero)
            {
                if (TitleOf(w) is { Length: > 0 } t) return t;
            }

            // No _NET_ACTIVE_WINDOW (bare WM): walk up from the focus window to the first titled parent.
            if (XGetInputFocus(_control, out var focus, out _) != 0 || focus == IntPtr.Zero) return null;
            for (var i = 0; i < 8 && focus != IntPtr.Zero; i++)
            {
                if (TitleOf(focus) is { Length: > 0 } t) return t;
                if (XQueryTree(_control, focus, out _, out var parent, out var children, out _) == 0) break;
                if (children != IntPtr.Zero) XFree(children);
                focus = parent;
            }
        }
        catch { /* title is best-effort; a miss only means the event is unfiltered */ }
        return null;
    }

    private IntPtr? ReadWindowProperty(IntPtr window, IntPtr atom)
    {
        if (XGetWindowProperty(_control, window, atom, 0, 1, false, IntPtr.Zero,
                out _, out _, out var nItems, out _, out var prop) != 0) return null;
        try { return nItems == 0 || prop == IntPtr.Zero ? null : Marshal.ReadIntPtr(prop); }
        finally { if (prop != IntPtr.Zero) XFree(prop); }
    }

    private string? TitleOf(IntPtr window)
    {
        foreach (var name in new[] { "_NET_WM_NAME", "WM_NAME" })
        {
            var atom = XInternAtom(_control, name, true);
            if (atom == IntPtr.Zero) continue;
            if (XGetWindowProperty(_control, window, atom, 0, 1024, false, IntPtr.Zero,
                    out _, out _, out var nItems, out _, out var prop) != 0) continue;
            try
            {
                if (nItems > 0 && prop != IntPtr.Zero)
                {
                    var s = Marshal.PtrToStringUTF8(prop);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            finally { if (prop != IntPtr.Zero) XFree(prop); }
        }
        return null;
    }

    private void FlushTyping()
    {
        if (_typing.Length == 0) return;
        Add(new InputEvent(InputEventType.TextInput, DateTime.UtcNow, _typingWindow, Button: _typing.ToString()));
        _typing.Clear();
        _typingWindow = null;
    }

    private void Add(InputEvent e) { lock (_events) _events.Add(e); }

    public void Dispose()
    {
        try { if (_context != IntPtr.Zero && _control != IntPtr.Zero) XRecordFreeContext(_control, _context); } catch { }
        try { if (_data != IntPtr.Zero) XCloseDisplay(_data); } catch { }
        try { if (_control != IntPtr.Zero) XCloseDisplay(_control); } catch { }
        _context = _data = _control = IntPtr.Zero;
    }
}
