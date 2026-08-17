namespace inpctl.Input;

/// <summary>
/// Translates inpctl's key notation into `cliclick` arguments (macOS). Modifiers are
/// held with kd:/ku: around the key; a letter/digit is typed with t:, a named key is
/// pressed with kp:. Pure and unit-tested — the injection lives in MacInputBackend.
///
/// UNVERIFIED end-to-end (no Mac to run cliclick on); the mapping itself is tested.
/// </summary>
internal static class MacKeys
{
    // our modifier -> cliclick modifier (Windows/super maps to Command).
    private static string? Modifier(string token) => token.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => "ctrl",
        "ALT" or "OPTION" => "alt",
        "SHIFT" => "shift",
        "WIN" or "LWIN" or "RWIN" or "SUPER" or "META" or "CMD" or "COMMAND" => "cmd",
        _ => null,
    };

    // our named key -> cliclick kp: name (null => not a known named key).
    private static string? Named(string token) => token.ToUpperInvariant() switch
    {
        "ENTER" or "RETURN" => "return",
        "TAB" => "tab",
        "ESC" or "ESCAPE" => "esc",
        "SPACE" => "space",
        "BACKSPACE" => "delete",       // cliclick "delete" is Backspace
        "DELETE" or "DEL" => "fwd-delete",
        "LEFT" => "arrow-left",
        "RIGHT" => "arrow-right",
        "UP" => "arrow-up",
        "DOWN" => "arrow-down",
        "HOME" => "home",
        "END" => "end",
        "PAGEUP" => "page-up",
        "PAGEDOWN" => "page-down",
        _ => null,
    };

    /// <summary>cliclick args for a chord/sequence, e.g. "CTRL+S" -> ["kd:ctrl","t:s","ku:ctrl"].</summary>
    public static List<string> Translate(string chord)
    {
        var args = new List<string>();
        foreach (var part in chord.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = part.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0) continue;

            var mods = new List<string>();
            for (var i = 0; i < tokens.Length - 1; i++)
                if (Modifier(tokens[i]) is { } m) mods.Add(m);

            var key = tokens[^1];
            var press = Named(key) is { } named ? $"kp:{named}"
                : key.Length == 1 ? $"t:{key.ToLowerInvariant()}"
                : Modifier(key) is not null ? null           // a lone modifier: skip
                : $"t:{key}";                                 // unknown: best-effort type
            if (press is null) continue;

            if (mods.Count > 0)
            {
                var modArg = string.Join(",", mods);
                args.Add($"kd:{modArg}");
                args.Add(press);
                args.Add($"ku:{modArg}");
            }
            else
            {
                args.Add(press);
            }
        }
        return args;
    }
}
