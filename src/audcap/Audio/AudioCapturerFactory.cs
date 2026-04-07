namespace audcap.Audio;

internal static class AudioCapturerFactory
{
    public static IAudioCapturer? Create(string platform) => platform switch
    {
        "Windows" => new WindowsAudioCapturer(),
        "Linux" => new LinuxAudioCapturer(),
        "macOS" => new MacAudioCapturer(),
        _ => null
    };
}
