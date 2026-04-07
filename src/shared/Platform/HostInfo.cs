using System.Runtime.InteropServices;

namespace IdleOps.Shared.Platform;

public enum HostPlatform
{
    Windows,
    MacOS,
    Linux,
    Unknown
}

public static class HostInfo
{
    public static HostPlatform Detect() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? HostPlatform.Windows :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? HostPlatform.MacOS :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? HostPlatform.Linux :
        HostPlatform.Unknown;

    public static string Describe(HostPlatform platform) => platform switch
    {
        HostPlatform.Windows => "Windows",
        HostPlatform.MacOS => "macOS",
        HostPlatform.Linux => "Linux",
        _ => "Unknown"
    };
}
