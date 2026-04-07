namespace vidcap.Video;

internal static class VideoCapturerFactory
{
    public static IVideoCapturer? Create(string platform) => platform switch
    {
        "Windows" => new WindowsVideoCapturer(),
        "Linux" => new LinuxVideoCapturer(),
        "macOS" => new MacVideoCapturer(),
        _ => null
    };
}
