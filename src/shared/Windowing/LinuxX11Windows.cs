using System.Text.RegularExpressions;
using IdleOps.Shared.Capture;

namespace IdleOps.Shared.Windowing;

/// <summary>
/// The single X11 window-search used across the toolkit (window locator, screen
/// capturer, and inpctl's Linux input backend) so the xdotool query lives in one
/// place instead of being copy-pasted per tool. Only meaningful on Linux (xdotool),
/// but not platform-annotated so the pure <see cref="ToRegex"/> helper stays testable.
/// </summary>
internal static class LinuxX11Windows
{
    /// <summary>Topmost visible window id matching the title pattern (supports * wildcards), or null.</summary>
    public static string? SearchId(string pattern)
    {
        var (ok, stdout, _) = ProcessRunner.Run("xdotool", "search", "--onlyvisible", "--name", ToRegex(pattern));
        if (!ok) return null;
        var ids = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return ids.Length > 0 ? ids[^1] : null;
    }

    /// <summary>Turn a `*`-wildcard title pattern into an xdotool (POSIX) regex, escaping the rest.</summary>
    public static string ToRegex(string pattern) => Regex.Escape(pattern).Replace("\\*", ".*");
}
