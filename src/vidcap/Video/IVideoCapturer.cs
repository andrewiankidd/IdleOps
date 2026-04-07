namespace vidcap.Video;

internal interface IVideoCapturer
{
    Task<int> CaptureAsync(string outputPath, string? windowTitle, CancellationToken token);
    string Platform { get; }
}
