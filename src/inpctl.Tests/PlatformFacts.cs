using System;
using Xunit;

namespace inpctl.Tests;

/// <summary>
/// A [Fact] that is skipped off Windows. Tag tests that exercise the Windows input
/// backend / InputSender (user32 P/Invoke like VkKeyScan) with [WindowsOnlyFact]; the
/// cross-platform logic (XdotoolKeys, OptionsParser) uses plain [Fact]/[Theory] and
/// runs everywhere, so the same suite is green on Windows and Linux CI.
/// </summary>
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only (user32 input backend).";
    }
}

/// <summary>A [Theory] that is skipped off Windows. See <see cref="WindowsOnlyFactAttribute"/>.</summary>
public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
{
    public WindowsOnlyTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only (user32 input backend).";
    }
}
