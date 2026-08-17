using IdleOps.Shared.Capture;

namespace audcap.Audio;

internal sealed class MacAudioCapturer : FfmpegAudioCapturer
{
    // macOS has no system-audio input device, so this captures whichever avfoundation
    // audio input is the loopback driver (BlackHole/Loopback/...) — resolved by name,
    // because index 0 is the built-in microphone on a stock Mac.
    public override string Platform => "macOS";

    protected override string BuildInputArguments()
    {
        return $"-f avfoundation -i \":{AvFoundationDevices.ResolveAudioIndex()}\"";
    }
}
