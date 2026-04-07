using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace audcap.Audio;

internal sealed class WindowsAudioCapturer : IAudioCapturer
{
    public string Platform => "Windows";

    public async Task<int> CaptureAsync(string outputPath, CancellationToken token)
    {
        var sanitizedOutput = Path.GetFullPath(outputPath);
        var outputDir = Path.GetDirectoryName(sanitizedOutput);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var capture = new WasapiLoopbackCapture();
        using var writer = new WaveFileWriter(sanitizedOutput, capture.WaveFormat);
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = 0; // guard against writes after stop

        capture.DataAvailable += (_, e) =>
        {
            if (e.BytesRecorded > 0 && Interlocked.CompareExchange(ref stopped, 0, 0) == 0)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        };

        capture.RecordingStopped += (_, e) =>
        {
            Interlocked.Exchange(ref stopped, 1);
            if (e.Exception != null)
            {
                tcs.TrySetException(e.Exception);
            }
            else
            {
                tcs.TrySetResult(0);
            }
        };

        using var registration = token.Register(() =>
        {
            try
            {
                if (capture.CaptureState != CaptureState.Stopped)
                {
                    capture.StopRecording();
                }
            }
            catch
            {
                // best-effort; capture may already be disposed
            }
        });

        Console.WriteLine($"Press Ctrl+C to stop capture. Writing to {sanitizedOutput}");
        capture.StartRecording();

        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Capture failed: {ex.Message}");
            return 1;
        }
    }
}
