namespace inpctl.Input;

/// <summary>
/// Picks the input backend for the current OS (mirrors audcap's
/// AudioCapturerFactory). Returns null on an unsupported platform.
/// </summary>
internal static class InputBackendFactory
{
    public static IInputBackend? Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsInputBackend();
        if (OperatingSystem.IsLinux()) return new LinuxInputBackend();
        if (OperatingSystem.IsMacOS()) return new MacInputBackend(); // UNVERIFIED (cliclick/osascript)
        return null;
    }
}
