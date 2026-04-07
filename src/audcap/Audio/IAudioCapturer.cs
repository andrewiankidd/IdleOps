namespace audcap.Audio;

internal interface IAudioCapturer
{
    Task<int> CaptureAsync(string outputPath, CancellationToken token);
    string Platform { get; }
}
