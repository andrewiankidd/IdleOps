namespace audcap.Audio;

internal sealed class LinuxAudioCapturer : FfmpegAudioCapturer
{
    // Captures from the default PulseAudio monitor device.
    public override string Platform => "Linux";

    protected override string BuildInputArguments()
    {
        return "-f pulse -i default";
    }
}
