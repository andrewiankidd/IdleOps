namespace vidcap.Video;

internal sealed class MacVideoCapturer : FfmpegVideoCapturer
{
    public override string Platform => "macOS";

    protected override string BuildInputArguments(string? windowTitle)
    {
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            Console.WriteLine("Warning: --window is currently only honored on Windows; capturing full display.");
        }

        // Index 1 commonly refers to the primary display when using avfoundation screen capture.
        return "-f avfoundation -framerate 30 -i \"1:none\" -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p";
    }
}
