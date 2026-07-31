using System.Runtime.Versioning;
using IdleOps.Shared.Logging;

namespace IdleOps.Shared.Platform;

/// <summary>
/// Central place for platform-gap handling so Windows-only capabilities fail
/// clearly and consistently on Linux/macOS instead of crashing obscurely (e.g. a
/// bare DllNotFoundException for user32.dll) or silently no-opping.
///
/// CLIs call <see cref="EnsureWindows"/> at startup and exit non-zero on failure.
/// Library APIs call <see cref="RequireWindows"/> and throw the idiomatic
/// <see cref="PlatformNotSupportedException"/> for a platform gap.
/// </summary>
public static class PlatformSupport
{
    private const string Suffix = "Windows-only; Linux/macOS support is not yet implemented.";

    /// <summary>
    /// For CLI entry points: returns true on Windows; otherwise logs a clean
    /// message to stderr and returns false so <c>Main</c> can return a non-zero code.
    /// </summary>
    [SupportedOSPlatformGuard("windows")]
    public static bool EnsureWindows(string component)
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }
        ConsoleLogger.Error($"[{component}] {Suffix}");
        return false;
    }

    /// <summary>
    /// For library APIs: throws <see cref="PlatformNotSupportedException"/> when
    /// called off Windows so the gap surfaces loudly at the call site.
    /// </summary>
    public static void RequireWindows(string component)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException($"{component} is {Suffix}");
        }
    }
}
