namespace IdleOps.Shared.Capture;

/// <summary>Result of a screen/window capture: success plus the saved image's pixel size.</summary>
public readonly record struct CaptureOutcome(bool Ok, int Width, int Height)
{
    public static readonly CaptureOutcome Failed = new(false, 0, 0);
}

/// <summary>
/// A platform's window/screen capturer. Windows uses GDI (PrintWindow); Linux (X11)
/// shells out to ImageMagick `import`; macOS uses the built-in `screencapture`.
/// Selected by <see cref="ScreenCapturerFactory"/> so callers stay platform-agnostic
/// (mirrors audcap's IAudioCapturer factory). Shared by scrcap, imgfnd, and any tool
/// that needs a cross-platform screenshot.
/// </summary>
public interface IScreenCapturer
{
    /// <summary>Human label for logs, e.g. "gdi" or "imagemagick (X11)".</summary>
    string Name { get; }

    /// <summary>
    /// Capture the window matching <paramref name="windowPattern"/> (wildcards allowed;
    /// "root"/"screen"/"desktop" means the whole display) to <paramref name="outputPath"/>.
    /// The image format is taken from the output extension.
    /// </summary>
    CaptureOutcome Capture(string windowPattern, string outputPath);
}
