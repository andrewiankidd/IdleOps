using System.Runtime.Versioning;
using IdleOps.Shared.Windows;

namespace IdleOps.Shared.Windowing;

/// <summary>Windows window presence via user32 (WindowMatcher / EnumWindows).</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsWindowLocator : IWindowLocator
{
    public string Name => "user32";

    public bool Exists(string pattern) => WindowMatcher.FindWindow(pattern) is not null;

    public string? ResolveTitle(string pattern) =>
        WindowMatcher.FindWindow(pattern, preferNewest: true)?.Title;

    public WindowBounds? GetBounds(string pattern)
    {
        var match = WindowMatcher.FindWindow(pattern, preferNewest: true);
        if (match is null) return null;
        var r = WindowMatcher.GetWindowBounds(match.Handle);
        return new WindowBounds(r.Left, r.Top, r.Width, r.Height);
    }

    public nint Resolve(string pattern) =>
        WindowMatcher.FindWindow(pattern, preferNewest: true)?.Handle ?? IntPtr.Zero;
}
