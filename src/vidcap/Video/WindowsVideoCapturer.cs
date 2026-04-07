using IdleOps.Shared.Windows;

namespace vidcap.Video;

internal sealed class WindowsVideoCapturer : FfmpegVideoCapturer
{
    public override string Platform => "Windows";

    protected override string BuildInputArguments(string? windowTitle)
    {
        var windowRequested = !string.IsNullOrWhiteSpace(windowTitle);
        if (windowRequested)
        {
            var match = WindowMatcher.FindWindow(windowTitle!);
            if (match is null)
            {
                throw new InvalidOperationException($"Window '{windowTitle}' not found.");
            }

            var rect = WindowMatcher.GetWindowBounds(match.Handle);
            if (rect.Width > 0 && rect.Height > 0)
            {
                Console.WriteLine($"Capturing window '{match.Title}' at {rect.Left},{rect.Top} size {rect.Width}x{rect.Height}.");
                var regionArg = $"-f gdigrab -framerate 30 -offset_x {rect.Left} -offset_y {rect.Top} -video_size {rect.Width}x{rect.Height} -i desktop";
                return $"{regionArg} -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p";
            }

            throw new InvalidOperationException($"Window '{windowTitle}' has invalid bounds.");
        }

        var sourceArg = "-f gdigrab -framerate 30 -i desktop";
        return $"{sourceArg} -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p";
    }
}
