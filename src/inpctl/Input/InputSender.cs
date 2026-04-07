using System.Runtime.InteropServices;
using IdleOps.Shared.Windows;

namespace inpctl.Input;

internal static class InputSender
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    public static bool SendKeyboard(string tokens, IntPtr hwnd)
    {
        var parts = tokens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var messages = new List<(int msg, ushort code, bool keyUp)>();

        foreach (var part in parts)
        {
            if (part.Contains('+', StringComparison.Ordinal))
            {
                var combo = part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var modifierKeys = combo[..^1];
                var key = combo[^1];

                foreach (var mod in modifierKeys)
                {
                    if (TryMapKey(mod, out var modCode))
                    {
                        messages.Add((IsAlt(modCode) ? WM_SYSKEYDOWN : WM_KEYDOWN, modCode, false));
                    }
                }

                if (TryMapKey(key, out var keyCode))
                {
                    messages.Add((IsAlt(keyCode) ? WM_SYSKEYDOWN : WM_KEYDOWN, keyCode, false));
                    messages.Add((IsAlt(keyCode) ? WM_SYSKEYUP : WM_KEYUP, keyCode, true));
                }

                foreach (var mod in modifierKeys.Reverse())
                {
                    if (TryMapKey(mod, out var modCode))
                    {
                        messages.Add((IsAlt(modCode) ? WM_SYSKEYUP : WM_KEYUP, modCode, true));
                    }
                }
            }
            else
            {
                if (TryMapKey(part, out var keyCode))
                {
                    messages.Add((IsAlt(keyCode) ? WM_SYSKEYDOWN : WM_KEYDOWN, keyCode, false));
                    messages.Add((IsAlt(keyCode) ? WM_SYSKEYUP : WM_KEYUP, keyCode, true));
                }
            }
        }

        if (messages.Count == 0)
        {
            return false;
        }

        foreach (var (msg, code, keyUp) in messages)
        {
            if (!PostMessage(hwnd, msg, code, keyUp ? 0xC0000001u : 0x00000001u))
            {
                Console.Error.WriteLine("Failed to send keyboard message.");
                return false;
            }
        }

        return true;
    }

    internal static bool TryMapKeyForTests(string token, out ushort keyCode) => TryMapKey(token, out keyCode);

    public static bool TypeText(string text, IntPtr hwnd)
    {
        foreach (var ch in text)
        {
            var vk = VkKeyScan(ch);
            if (vk == 0xFFFF)
            {
                Console.Error.WriteLine($"Cannot map character '{ch}'.");
                return false;
            }

            var vkCode = (ushort)(vk & 0xFF);
            var shiftState = (vk >> 8) & 0xFF;

            if ((shiftState & 1) != 0 && !PostMessage(hwnd, WM_KEYDOWN, 0x10, 0x00000001u)) return false;
            if ((shiftState & 2) != 0 && !PostMessage(hwnd, WM_KEYDOWN, 0x11, 0x00000001u)) return false;
            if ((shiftState & 4) != 0 && !PostMessage(hwnd, WM_SYSKEYDOWN, 0x12, 0x20000001u)) return false;

            if (!PostMessage(hwnd, (shiftState & 4) != 0 ? WM_SYSKEYDOWN : WM_KEYDOWN, vkCode, 0x00000001u))
            {
                Console.Error.WriteLine("Failed to send key down.");
                return false;
            }

            if (!PostMessage(hwnd, WM_CHAR, (ushort)ch, 0x00000001u))
            {
                Console.Error.WriteLine("Failed to send character.");
                return false;
            }

            if (!PostMessage(hwnd, (shiftState & 4) != 0 ? WM_SYSKEYUP : WM_KEYUP, vkCode, 0xC0000001u))
            {
                Console.Error.WriteLine("Failed to send key up.");
                return false;
            }

            if ((shiftState & 4) != 0 && !PostMessage(hwnd, WM_SYSKEYUP, 0x12, 0xC0000001u)) return false;
            if ((shiftState & 2) != 0 && !PostMessage(hwnd, WM_KEYUP, 0x11, 0xC0000001u)) return false;
            if ((shiftState & 1) != 0 && !PostMessage(hwnd, WM_KEYUP, 0x10, 0xC0000001u)) return false;
        }

        return true;
    }

    public static bool SendMouse(string coords, IntPtr hwnd, MouseButton button, bool moveCursor)
    {
        var segments = coords.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Length > 2)
        {
            Console.Error.WriteLine("Invalid mouse coords. Expected 'x,y' or 'x1,y1-x2,y2'.");
            return false;
        }

        var (windowLeft, windowTop, windowWidth, windowHeight) = (0, 0, 0, 0);
        if (hwnd != IntPtr.Zero)
        {
            var rect = WindowMatcher.GetWindowBounds(hwnd);
            windowLeft = rect.Left;
            windowTop = rect.Top;
            windowWidth = rect.Width;
            windowHeight = rect.Height;
        }

        if (!TryParsePoint(segments[0], windowWidth, windowHeight, out var startX, out var startY))
        {
            Console.Error.WriteLine("Invalid start coordinate.");
            return false;
        }

        var hasEnd = segments.Length == 2;
        int endX = startX, endY = startY;
        if (hasEnd && !TryParsePoint(segments[1], windowWidth, windowHeight, out endX, out endY))
        {
            Console.Error.WriteLine("Invalid end coordinate.");
            return false;
        }

        var screenWidth = GetSystemMetrics(0);
        var screenHeight = GetSystemMetrics(1);
        if (screenWidth <= 1 || screenHeight <= 1)
        {
            Console.Error.WriteLine("Invalid screen metrics.");
            return false;
        }

        int ToAbsX(int relX) => (int)Math.Clamp((windowWidth > 0 ? windowLeft + relX : relX) * 65535.0 / (screenWidth - 1), 0, 65535);
        int ToAbsY(int relY) => (int)Math.Clamp((windowHeight > 0 ? windowTop + relY : relY) * 65535.0 / (screenHeight - 1), 0, 65535);

        var absStartX = ToAbsX(startX);
        var absStartY = ToAbsY(startY);
        var absEndX = ToAbsX(endX);
        var absEndY = ToAbsY(endY);
        var pixelStartX = windowWidth > 0 ? windowLeft + startX : startX;
        var pixelStartY = windowHeight > 0 ? windowTop + startY : startY;
        var pixelEndX = windowWidth > 0 ? windowLeft + endX : endX;
        var pixelEndY = windowHeight > 0 ? windowTop + endY : endY;

        var (downFlag, upFlag) = button switch
        {
            MouseButton.Left => (MouseEventFlags.LEFTDOWN, MouseEventFlags.LEFTUP),
            MouseButton.Right => (MouseEventFlags.RIGHTDOWN, MouseEventFlags.RIGHTUP),
            MouseButton.Middle => (MouseEventFlags.MIDDLEDOWN, MouseEventFlags.MIDDLEUP),
            _ => (MouseEventFlags.LEFTDOWN, MouseEventFlags.LEFTUP)
        };

        var inputs = new List<INPUT>
        {
            BuildMouse(absStartX, absStartY, MouseEventFlags.MOVE | MouseEventFlags.ABSOLUTE),
            BuildMouse(absStartX, absStartY, downFlag | MouseEventFlags.ABSOLUTE)
        };

        if (hasEnd)
        {
            inputs.Add(BuildMouse(absEndX, absEndY, MouseEventFlags.MOVE | MouseEventFlags.ABSOLUTE));
            inputs.Add(BuildMouse(absEndX, absEndY, upFlag | MouseEventFlags.ABSOLUTE));
        }
        else
        {
            inputs.Add(BuildMouse(absStartX, absStartY, upFlag | MouseEventFlags.ABSOLUTE));
        }

        if (moveCursor)
        {
            SetCursorPos(pixelStartX, pixelStartY);
            if (hasEnd) SetCursorPos(pixelEndX, pixelEndY);
        }

        return Send(inputs);
    }

    private static bool Send(List<INPUT> inputs)
    {
        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count)
        {
            Console.Error.WriteLine("Failed to send input.");
            return false;
        }
        return true;
    }

    private static bool TryParseCoord(string token, int windowSize, int screenSize, out int value)
    {
        value = 0;
        if (token.Contains('%'))
        {
            var trimmed = token.Replace("%", string.Empty).Trim();
            if (!double.TryParse(trimmed, out var pct)) return false;
            var basis = windowSize > 0 ? windowSize : screenSize;
            value = (int)Math.Round(basis * (pct / 100.0), MidpointRounding.AwayFromZero);
            return true;
        }
        return int.TryParse(token, out value);
    }

    private static bool TryParsePoint(string token, int windowWidth, int windowHeight, out int x, out int y)
    {
        x = y = 0;
        var pieces = token.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pieces.Length != 2) return false;
        var screenWidth = GetSystemMetrics(0);
        var screenHeight = GetSystemMetrics(1);
        return TryParseCoord(pieces[0], windowWidth, screenWidth, out x)
            && TryParseCoord(pieces[1], windowHeight, screenHeight, out y);
    }

    private static INPUT BuildMouse(int absX, int absY, MouseEventFlags flags) => new()
    {
        type = 0,
        U = new InputUnion { mi = new MOUSEINPUT { dx = absX, dy = absY, dwFlags = flags, dwExtraInfo = UIntPtr.Zero } }
    };

    internal static bool TryMapKey(string token, out ushort keyCode)
    {
        keyCode = token.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => 0x11,
            "ALT" => 0x12,
            "SHIFT" => 0x10,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "BACKSPACE" => 0x08,
            "DELETE" => 0x2E,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            _ => 0
        };

        if (keyCode != 0) return true;
        if (token.Length == 1) { keyCode = VkKeyScan(token[0]); return keyCode != 0xFFFF; }
        return false;
    }

    private static bool IsAlt(ushort keyCode) => keyCode == 0x12;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int Msg, ushort wParam, uint lParam);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern ushort VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
}
