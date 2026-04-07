namespace stpcap.Recording;

internal enum InputEventType
{
    KeyDown,
    KeyUp,
    MouseClick,
    MouseDrag,
    TextInput
}

internal sealed record InputEvent(
    InputEventType Type,
    DateTime Timestamp,
    string? WindowTitle,
    // Keyboard
    ushort VirtualKey = 0,
    char Character = '\0',
    // Mouse
    int X = 0,
    int Y = 0,
    int EndX = 0,
    int EndY = 0,
    string Button = "left");
