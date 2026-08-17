namespace IdleOps.Shared.Platform;

/// <summary>
/// macOS gates screen capture and synthetic input behind TCC privacy permissions, and
/// the tools we shell out to report a denial in ways that are easy to mistake for an
/// ordinary "not found" result:
///
/// <list type="bullet">
/// <item><c>cliclick</c> prints a warning to stderr and still <b>exits 0</b> having done
/// nothing, so an ungranted run looks like a successful click.</item>
/// <item><c>osascript</c> fails with error -1743 (not authorized) or -1712 (the Apple
/// event timed out waiting on the consent prompt), which reads as "window not found"
/// once the error text is discarded.</item>
/// <item><c>screencapture</c> fails with "could not create image from display", which
/// says nothing about permissions.</item>
/// </list>
///
/// These helpers recognize those signatures so each backend can fail loudly with an
/// actionable message instead of silently no-opping. Verified against macOS 26 with
/// both permissions withheld.
/// </summary>
public static class MacPermissions
{
    /// <summary>Told to the user when synthetic input / window scripting is blocked.</summary>
    public const string AccessibilityHint =
        "macOS Accessibility permission is not granted, so input and window scripting are ignored. " +
        "Grant it to the app that runs this tool (Terminal, iTerm, or your IDE) under " +
        "System Settings > Privacy & Security > Accessibility, then retry.";

    /// <summary>Told to the user when a capture is blocked.</summary>
    public const string ScreenRecordingHint =
        "macOS Screen Recording permission is not granted, so the display cannot be captured. " +
        "Grant it to the app that runs this tool (Terminal, iTerm, or your IDE) under " +
        "System Settings > Privacy & Security > Screen Recording, then restart that app.";

    /// <summary>True when tool output carries a macOS Accessibility denial.</summary>
    public static bool IndicatesAccessibilityDenied(string? output) =>
        output is { Length: > 0 } &&
        (output.Contains("Accessibility privileges not enabled", StringComparison.OrdinalIgnoreCase) // cliclick
         || output.Contains("-1743", StringComparison.Ordinal)                                       // osascript: not authorized
         || output.Contains("-1712", StringComparison.Ordinal)                                       // osascript: consent prompt timed out
         || output.Contains("-25211", StringComparison.Ordinal)                                      // osascript: assistive access refused
         || output.Contains("not allowed assistive access", StringComparison.OrdinalIgnoreCase));

    /// <summary>True when tool output carries a macOS Screen Recording denial.</summary>
    public static bool IndicatesScreenRecordingDenied(string? output) =>
        output is { Length: > 0 } &&
        output.Contains("could not create image from display", StringComparison.OrdinalIgnoreCase);
}
