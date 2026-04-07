namespace vidcap.Video;

internal sealed class LinuxVideoCapturer : FfmpegVideoCapturer
{
    public override string Platform => "Linux";

    protected override string BuildInputArguments(string? windowTitle)
    {
        var display = Environment.GetEnvironmentVariable("DISPLAY") ?? ":0.0";
        var sourceArg = $"-f x11grab -framerate 30 -i {display}";
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            Console.WriteLine("Warning: --window is currently only honored on Windows; capturing full display.");
        }

        return $"{sourceArg} -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p";
    }
}
