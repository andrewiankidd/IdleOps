namespace IdleOps.Shared.Windowing;

/// <summary>Window rectangle in screen pixels.</summary>
public readonly record struct WindowBounds(int X, int Y, int Width, int Height);

/// <summary>
/// Cross-platform window presence lookup by title pattern. Windows uses user32
/// (WindowMatcher); Linux (X11) uses xdotool. Selected by
/// <see cref="WindowLocatorFactory"/> so tools like waitfr stay platform-agnostic.
/// </summary>
public interface IWindowLocator
{
    /// <summary>Human label for logs, e.g. "user32" or "xdotool (X11)".</summary>
    string Name { get; }

    /// <summary>True if any visible window matches the pattern (supports * wildcards).</summary>
    bool Exists(string pattern);

    /// <summary>The title of the best-matching window, or null if none.</summary>
    string? ResolveTitle(string pattern);

    /// <summary>Screen-pixel bounds of the best-matching window, or null if none.</summary>
    WindowBounds? GetBounds(string pattern);

    /// <summary>The raw platform handle of the best-matching window (HWND / X11 id), or 0 if none.</summary>
    nint Resolve(string pattern);
}

/// <summary>Picks the window locator for the current OS. Null on an unsupported platform (e.g. macOS).</summary>
public static class WindowLocatorFactory
{
    public static IWindowLocator? Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsWindowLocator();
        if (OperatingSystem.IsLinux()) return new LinuxWindowLocator();
        if (OperatingSystem.IsMacOS()) return new MacWindowLocator(); // UNVERIFIED (osascript)
        return null;
    }
}
