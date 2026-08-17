namespace IdleOps.Shared.Capture;

/// <summary>Picks the screen capturer for the current OS. Null on an unsupported platform.</summary>
public static class ScreenCapturerFactory
{
    public static IScreenCapturer? Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsScreenCapturer();
        if (OperatingSystem.IsLinux()) return new LinuxScreenCapturer();
        if (OperatingSystem.IsMacOS()) return new MacScreenCapturer();
        return null;
    }

    /// <summary>True when the pattern means "the whole display" rather than a specific window.</summary>
    public static bool IsWholeScreen(string pattern) =>
        pattern is "root" or "screen" or "desktop" or "*";
}
