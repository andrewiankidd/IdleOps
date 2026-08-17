namespace uiactl.Uia;

/// <summary>Picks the accessibility backend for the current OS. Null on unsupported (e.g. macOS).</summary>
internal static class UiAutomationFactory
{
    public static IUiAutomation? Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsUiAutomation();
        if (OperatingSystem.IsLinux()) return new LinuxUiAutomation();
        if (OperatingSystem.IsMacOS()) return new MacUiAutomation(); // UNVERIFIED (osascript UI scripting)
        return null;
    }
}
