using System.Globalization;

namespace inpctl.Input;

/// <summary>
/// Parses mouse coordinate specs into window-relative pixel points. Pure and
/// platform-neutral, so the Linux (xdotool) and macOS (cliclick) backends share it.
/// </summary>
internal static class MouseCoords
{
    /// <summary>Parse "x,y" / "50%,50%" / "x1,y1-x2,y2" into 1 (click) or 2 (drag) points.</summary>
    public static bool TryParse(string coords, WindowBounds? bounds, out List<(int x, int y)> points)
    {
        points = new List<(int, int)>();
        var segments = coords.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Length > 2) return false;

        foreach (var seg in segments)
        {
            var pieces = seg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length != 2) return false;
            if (!TryCoord(pieces[0], bounds?.Width ?? 0, out var x)) return false;
            if (!TryCoord(pieces[1], bounds?.Height ?? 0, out var y)) return false;
            points.Add((x, y));
        }
        return true;
    }

    private static bool TryCoord(string token, int span, out int value)
    {
        value = 0;
        if (token.Contains('%'))
        {
            if (!double.TryParse(token.Replace("%", string.Empty).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                return false;
            value = (int)Math.Round(span * (pct / 100.0), MidpointRounding.AwayFromZero);
            return true;
        }
        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
