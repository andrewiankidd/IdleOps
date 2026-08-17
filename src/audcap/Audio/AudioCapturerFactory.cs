namespace audcap.Audio;

internal static class AudioCapturerFactory
{
    // The macOS arm re-checks the real OS rather than trusting the caller's string: the
    // backend is annotated [SupportedOSPlatform("macos")], and a runtime guard is what
    // lets the analyzer see that (a string comparison tells it nothing). It is also
    // simply truer — a mismatched string now yields "no capturer" instead of a backend
    // shelling out to tools that do not exist.
    public static IAudioCapturer? Create(string platform) => platform switch
    {
        "Windows" => new WindowsAudioCapturer(),
        "Linux" => new LinuxAudioCapturer(),
        "macOS" when OperatingSystem.IsMacOS() => new MacAudioCapturer(),
        _ => null
    };
}
