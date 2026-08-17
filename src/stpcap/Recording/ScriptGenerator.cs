using System.Text;
using IdleOps.Shared.Windowing;
using IdleOps.Shared.Windows.Uia;

namespace stpcap.Recording;

internal static class ScriptGenerator
{
    // Cross-platform window bounds (user32 on Windows, xdotool on Linux).
    private static readonly IWindowLocator? Locator = WindowLocatorFactory.Create();

    private static readonly Dictionary<ushort, string> VkNames = new()
    {
        [0x08] = "BACKSPACE", [0x09] = "TAB", [0x0D] = "ENTER", [0x10] = "SHIFT",
        [0x11] = "CTRL", [0x12] = "ALT", [0x1B] = "ESCAPE", [0x20] = "SPACE",
        [0x25] = "LEFT", [0x26] = "UP", [0x27] = "RIGHT", [0x28] = "DOWN",
        [0x2E] = "DELETE", [0x24] = "HOME", [0x23] = "END",
        [0x21] = "PAGEUP", [0x22] = "PAGEDOWN",
        [0x70] = "F1", [0x71] = "F2", [0x72] = "F3", [0x73] = "F4",
        [0x74] = "F5", [0x75] = "F6", [0x76] = "F7", [0x77] = "F8",
        [0x78] = "F9", [0x79] = "F10", [0x7A] = "F11", [0x7B] = "F12"
    };

    public static string Generate(IReadOnlyList<InputEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Recorded by stpcap");
        sb.AppendLine("steps:");

        DateTime? lastTime = null;

        for (var i = 0; i < events.Count; i++)
        {
            var evt = events[i];

            // Insert sleep between events if gap > 500ms
            if (lastTime is not null)
            {
                var gap = (evt.Timestamp - lastTime.Value).TotalSeconds;
                if (gap > 0.5)
                {
                    sb.AppendLine($"  - name: Wait {gap:0.#}s");
                    sb.AppendLine($"    action: sleep");
                    sb.AppendLine($"    args: \"{gap:0.#}\"");
                    sb.AppendLine();
                }
            }

            var windowArg = evt.WindowTitle is not null ? $"    window: \"{EscapeYaml(evt.WindowTitle)}\"" : null;

            switch (evt.Type)
            {
                case InputEventType.TextInput:
                    sb.AppendLine($"  - name: Type text");
                    sb.AppendLine($"    action: exec");
                    sb.AppendLine($"    args: inpctl --window \"{EscapeYaml(evt.WindowTitle ?? "*")}\" --type \"{EscapeYaml(evt.Button)}\"");
                    sb.AppendLine($"    wait: true");
                    break;

                case InputEventType.KeyDown:
                    // Look ahead for key combo pattern (modifier down + key down + key up + modifier up)
                    var keyName = VkToName(evt.VirtualKey);
                    if (keyName is not null && !IsModifier(evt.VirtualKey))
                    {
                        // Check if preceded by modifier downs
                        var combo = BuildCombo(events, i);
                        sb.AppendLine($"  - name: Press {combo}");
                        sb.AppendLine($"    action: exec");
                        sb.AppendLine($"    args: inpctl --window \"{EscapeYaml(evt.WindowTitle ?? "*")}\" --keyboard \"{combo}\"");
                        sb.AppendLine($"    wait: true");
                    }
                    break;

                case InputEventType.MouseClick:
                    // Prefer a resilient semantic step (UIA action, then OCR click-text)
                    // for left clicks; fall back to raw coordinates.
                    if (evt.Button == "left" && TryAppendSemanticClick(sb, evt))
                    {
                        break;
                    }
                    var btn = evt.Button == "right" ? "--rightmouse" : evt.Button == "middle" ? "--middlemouse" : "--leftmouse";
                    // Convert screen coords to window-relative
                    var (relX, relY) = ToWindowRelative(evt.WindowTitle, evt.X, evt.Y);
                    sb.AppendLine($"  - name: Click at ({relX},{relY})");
                    sb.AppendLine($"    action: exec");
                    sb.AppendLine($"    args: inpctl --window \"{EscapeYaml(evt.WindowTitle ?? "*")}\" {btn} \"{relX},{relY}\"");
                    sb.AppendLine($"    wait: true");
                    break;

                case InputEventType.MouseDrag:
                    var (sx, sy) = ToWindowRelative(evt.WindowTitle, evt.X, evt.Y);
                    var (ex, ey) = ToWindowRelative(evt.WindowTitle, evt.EndX, evt.EndY);
                    sb.AppendLine($"  - name: Drag from ({sx},{sy}) to ({ex},{ey})");
                    sb.AppendLine($"    action: exec");
                    sb.AppendLine($"    args: inpctl --window \"{EscapeYaml(evt.WindowTitle ?? "*")}\" --leftmouse \"{sx},{sy}-{ex},{ey}\" --move-cursor");
                    sb.AppendLine($"    wait: true");
                    break;
            }

            sb.AppendLine();
            lastTime = evt.Timestamp;
        }

        return sb.ToString();
    }

    // Emit a resilient step for a left click when we captured a UIA element:
    //   1. a semantic control action (invoke/select/toggle/expand) by selector, else
    //   2. an OCR click-text by the element's visible label, else
    //   3. return false so the caller falls back to raw coordinates.
    internal static bool TryAppendSemanticClick(StringBuilder sb, InputEvent evt)
    {
        var el = evt.Element;
        if (el is null) return false;
        var win = EscapeYaml(evt.WindowTitle ?? "*");

        if (el.ClickVerb is not null && el.HasSelector)
        {
            var (field, value) = !string.IsNullOrEmpty(el.AutomationId)
                ? ("automation_id", el.AutomationId!)
                : ("element", el.Name!);
            var label = el.AutomationId ?? el.Name ?? el.ControlType;
            sb.AppendLine($"  - name: {Capitalize(el.ClickVerb)} {label}");
            sb.AppendLine($"    action: {el.ClickVerb}");
            sb.AppendLine($"    window: \"{win}\"");
            sb.AppendLine($"    {field}: \"{EscapeYaml(value)}\"");
            return true;
        }

        if (!string.IsNullOrEmpty(el.Name))
        {
            sb.AppendLine($"  - name: Click \"{el.Name}\"");
            sb.AppendLine($"    action: click-text");
            sb.AppendLine($"    window: \"{win}\"");
            sb.AppendLine($"    text: \"{EscapeYaml(el.Name)}\"");
            return true;
        }

        return false;
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string BuildCombo(IReadOnlyList<InputEvent> events, int keyIndex)
    {
        var parts = new List<string>();

        // Look backward for recent modifier key-downs (within last few events)
        for (var j = keyIndex - 1; j >= Math.Max(0, keyIndex - 6); j--)
        {
            if (events[j].Type == InputEventType.KeyDown && IsModifier(events[j].VirtualKey))
            {
                var name = VkToName(events[j].VirtualKey);
                if (name is not null && !parts.Contains(name))
                {
                    parts.Insert(0, name);
                }
            }
            else break;
        }

        var keyName = VkToName(events[keyIndex].VirtualKey) ?? $"0x{events[keyIndex].VirtualKey:X2}";
        parts.Add(keyName);

        return string.Join("+", parts);
    }

    private static bool IsModifier(ushort vk) => vk is 0x10 or 0x11 or 0x12;

    private static string? VkToName(ushort vk)
    {
        if (VkNames.TryGetValue(vk, out var name)) return name;
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString(); // 0-9
        if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString(); // A-Z
        return null;
    }

    private static (int x, int y) ToWindowRelative(string? windowTitle, int screenX, int screenY)
    {
        if (windowTitle is null || Locator is null) return (screenX, screenY);
        var bounds = Locator.GetBounds(windowTitle);
        if (bounds is not { } b) return (screenX, screenY);
        return (screenX - b.X, screenY - b.Y);
    }

    private static string EscapeYaml(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
