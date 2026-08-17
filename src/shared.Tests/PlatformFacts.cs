using Xunit;

namespace IdleOps.Shared.Tests;

/// <summary>
/// A [Fact] skipped off Windows — for tests that P/Invoke user32 (WindowMatcher's
/// EnumWindows/GetWindowText). Pure logic (wildcard regex, OCR location/parsing)
/// uses plain [Fact]/[Theory] so the same suite is green on Windows and Linux CI.
/// </summary>
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only (user32).";
    }
}
