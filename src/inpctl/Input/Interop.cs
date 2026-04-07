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

internal enum MouseButton
{
    Left,
    Right,
    Middle
}
