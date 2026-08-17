namespace vidcap.Video;

internal static class VideoCapturerFactory
{
    // See AudioCapturerFactory: the macOS arm re-checks the real OS so the analyzer can
    // see the [SupportedOSPlatform("macos")] backend is only constructed on macOS.
    public static IVideoCapturer? Create(string platform) => platform switch
    {
        "Windows" => new WindowsVideoCapturer(),
        "Linux" => new LinuxVideoCapturer(),
        "macOS" when OperatingSystem.IsMacOS() => new MacVideoCapturer(),
        _ => null
    };
}
