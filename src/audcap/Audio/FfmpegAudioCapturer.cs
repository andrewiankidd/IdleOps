using System.Diagnostics;

namespace audcap.Audio;

internal abstract class FfmpegAudioCapturer : IAudioCapturer
{
    public abstract string Platform { get; }

    protected abstract string BuildInputArguments();

    public async Task<int> CaptureAsync(string outputPath, CancellationToken token)
    {
        var sanitizedOutput = Path.GetFullPath(outputPath);
        var outputDir = Path.GetDirectoryName(sanitizedOutput);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var arguments = $"{BuildInputArguments()} \"{sanitizedOutput}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ffmpeg process.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Could not start ffmpeg. Ensure ffmpeg is installed and available on PATH.", ex);
        }

        using (process)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is { Length: > 0 })
                {
                    Console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is { Length: > 0 })
                {
                    Console.Error.WriteLine(e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Console.WriteLine($"Press Ctrl+C to stop capture. Writing to {sanitizedOutput}");

            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }

                throw;
            }

            return process.ExitCode;
        }
    }
}
