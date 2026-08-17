using System.Runtime.Versioning;
using IdleOps.Shared.Capture;

namespace vidcap.Video;

[SupportedOSPlatform("macos")]
internal sealed class MacVideoCapturer : FfmpegVideoCapturer
{
    public override string Platform => "macOS";

    protected override string BuildInputArguments(string? windowTitle)
    {
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            Console.WriteLine("Warning: --window is currently only honored on Windows; capturing full display.");
        }

        // The screen's index is machine-specific (cameras enumerate first), so ask
        // ffmpeg which one it is rather than assuming.
        var screen = AvFoundationDevices.ResolveScreenIndex();
        return $"-f avfoundation -framerate 30 -i \"{screen}:none\" -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p";
    }
}
