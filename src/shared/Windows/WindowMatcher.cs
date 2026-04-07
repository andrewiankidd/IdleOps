using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace IdleOps.Shared.Windows;

public static class WindowMatcher
{
    /// <summary>
    /// Convert a simple wildcard pattern (using *) to a compiled regex.
    /// Examples: "Notepad*", "*Chrome*", "My*App"
    /// </summary>
    public static Regex BuildWildcardRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern.Trim());
        var regexPattern = "^" + escaped.Replace("\\*", ".*") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Find the first visible window matching the title pattern.
    /// </summary>
    public static WindowInfo? FindWindow(string titlePattern)
    {
        return FindWindow(titlePattern, preferNewest: false);
    }

    /// <summary>
    /// Find a visible window matching the title pattern.
    /// When preferNewest is true, returns the most recently started process among matches.
    /// </summary>
    public static WindowInfo? FindWindow(string titlePattern, bool preferNewest)
    {
        var matches = FindAllWindows(titlePattern);
        if (matches.Count == 0)
        {
            return null;
        }

        if (!preferNewest)
        {
            return matches[0];
        }

        WindowInfo? best = null;
        var bestStart = DateTime.MinValue;
        foreach (var info in matches)
        {
            var started = TryGetStartTime(info.ProcessId);
            if (started > bestStart)
            {
                bestStart = started;
                best = info;
            }
        }

        return best ?? matches[0];
    }

    /// <summary>
    /// Return all visible windows matching the title pattern.
    /// </summary>
    public static IReadOnlyList<WindowInfo> FindAllWindows(string titlePattern)
    {
        var regex = BuildWildcardRegex(titlePattern);
        var results = new List<WindowInfo>();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd))
            {
                return true;
            }

            var title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            if (regex.IsMatch(title))
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
                results.Add(new WindowInfo(hwnd, title, pid));
            }

            return true;
        }, IntPtr.Zero);

        return results;
    }

    /// <summary>
    /// Get the bounding rectangle of a window.
    /// </summary>
    public static RECT GetWindowBounds(IntPtr handle)
    {
        NativeMethods.GetWindowRect(handle, out var rect);
        return rect;
    }

    public static string GetWindowTitle(IntPtr hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);
        if (length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static DateTime TryGetStartTime(uint pid)
    {
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.StartTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
