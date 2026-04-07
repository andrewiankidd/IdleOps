namespace audcap.Audio;

internal sealed class MacAudioCapturer : FfmpegAudioCapturer
{
    // Captures from the default macOS system audio device (requires a loopback driver such as BlackHole).
    public override string Platform => "macOS";

    protected override string BuildInputArguments()
    {
        return "-f avfoundation -i \":0\"";
    }
}
