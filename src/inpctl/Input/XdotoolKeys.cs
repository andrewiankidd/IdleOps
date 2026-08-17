namespace inpctl.Input;

/// <summary>
/// Translates inpctl's key notation ("CTRL+S", "WIN+D", "CTRL+A, DELETE") into
/// xdotool key specs. Comma-separated parts become separate keystrokes; each part's
/// modifiers and key are joined with '+' using xdotool/X11 keysym names. Pure and
/// unit-tested — the runtime injection lives in <see cref="LinuxInputBackend"/>.
/// </summary>
internal static class XdotoolKeys
{
    /// <summary>Map one inpctl token to its xdotool/X11 keysym name.</summary>
    public static string MapToken(string token) => token.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => "ctrl",
        "ALT" => "alt",
        "SHIFT" => "shift",
        "WIN" or "LWIN" or "RWIN" or "SUPER" or "META" => "super",
        "APPS" or "MENU" or "CONTEXT" => "Menu",
        "ENTER" or "RETURN" => "Return",
        "TAB" => "Tab",
        "ESC" or "ESCAPE" => "Escape",
        "SPACE" => "space",
        "BACKSPACE" => "BackSpace",
        "DELETE" or "DEL" => "Delete",
        "INSERT" or "INS" => "Insert",
        "CAPS" or "CAPSLOCK" => "Caps_Lock",
        "LEFT" => "Left",
        "UP" => "Up",
        "RIGHT" => "Right",
        "DOWN" => "Down",
        "HOME" => "Home",
        "END" => "End",
        // X11 keysyms for the paging keys are Prior/Next (not Page_Up/Page_Down).
        "PAGEUP" => "Prior",
        "PAGEDOWN" => "Next",
        "F1" => "F1", "F2" => "F2", "F3" => "F3", "F4" => "F4",
        "F5" => "F5", "F6" => "F6", "F7" => "F7", "F8" => "F8",
        "F9" => "F9", "F10" => "F10", "F11" => "F11", "F12" => "F12",
        // A single letter: lowercase it so "CTRL+S" becomes ctrl+s (not ctrl+shift+s,
        // which is what the uppercase "S" keysym would mean to xdotool).
        _ when token.Length == 1 && char.IsLetter(token[0]) => token.ToLowerInvariant(),
        // Any other single character (digit/symbol) passes through as-is.
        _ when token.Length == 1 => token,
        _ => token, // unknown multi-char token: pass through, let xdotool decide
    };

    /// <summary>
    /// Translate a full chord/sequence into one xdotool key spec per comma-separated
    /// part, e.g. "CTRL+A, DELETE" -> ["ctrl+a", "Delete"].
    /// </summary>
    public static List<string> Translate(string chord)
    {
        var specs = new List<string>();
        foreach (var part in chord.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0) continue;
            specs.Add(string.Join('+', tokens.Select(MapToken)));
        }
        return specs;
    }
}
