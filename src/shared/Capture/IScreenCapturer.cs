namespace IdleOps.Shared.Capture;

/// <summary>
/// Result of a screen/window capture: success, the saved image's pixel size, and how
/// many image pixels make up one unit of the window/input coordinate space.
///
/// <see cref="Scale"/> is 1.0 everywhere except macOS Retina displays, where
/// `screencapture` writes native pixels while window bounds and synthetic input are
/// expressed in points — a factor of 2 between "where OCR found the text" and "where a
/// click has to go". Anything converting image coordinates into click coordinates must
/// divide by it.
/// </summary>
public readonly record struct CaptureOutcome(bool Ok, int Width, int Height, double Scale = 1.0)
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
