using System.Runtime.InteropServices;

namespace inpctl.Input;

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint type;
    public InputUnion U;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public MouseEventFlags dwFlags;
    public uint time;
    public UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public KeyEventFlags dwFlags;
    public uint time;
    public UIntPtr dwExtraInfo;
}

[Flags]
internal enum KeyEventFlags : uint
{
    EXTENDEDKEY = 0x0001,
    KEYUP = 0x0002,
    UNICODE = 0x0004,
    SCANCODE = 0x0008
}

// GetGUIThreadInfo output — used to resolve the focused child window of a
// target thread when posting input to a background window (see Program.ResolveInputTarget).
[StructLayout(LayoutKind.Sequential)]
internal struct GUITHREADINFO
{
    public uint cbSize;
    public uint flags;
    public IntPtr hwndActive;
    public IntPtr hwndFocus;
    public IntPtr hwndCapture;
    public IntPtr hwndMenuOwner;
    public IntPtr hwndMoveSize;
    public IntPtr hwndCaret;
    public int rcCaretLeft;
    public int rcCaretTop;
    public int rcCaretRight;
    public int rcCaretBottom;
}

[Flags]
internal enum MouseEventFlags : uint
{
    MOVE = 0x0001,
    LEFTDOWN = 0x0002,
    LEFTUP = 0x0004,
    RIGHTDOWN = 0x0008,
    RIGHTUP = 0x0010,
    MIDDLEDOWN = 0x0020,
    MIDDLEUP = 0x0040,
    ABSOLUTE = 0x8000
}
