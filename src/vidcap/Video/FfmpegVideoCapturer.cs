using System.Diagnostics;

namespace vidcap.Video;

internal abstract class FfmpegVideoCapturer : IVideoCapturer
{
    public abstract string Platform { get; }

    protected abstract string BuildInputArguments(string? windowTitle);

    public async Task<int> CaptureAsync(string outputPath, string? windowTitle, CancellationToken token)
    {
        var sanitizedOutput = Path.GetFullPath(outputPath);
        var outputDir = Path.GetDirectoryName(sanitizedOutput);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var inputArgs = BuildInputArguments(windowTitle);
        var arguments = $"{inputArgs} \"{sanitizedOutput}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
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

        using var _ = process;
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

        using var registration = token.Register(() => TryGracefulStop(process));

        try
        {
            await process.WaitForExitAsync(token);
        }
        catch (OperationCanceledException)
        {
            // token fired — TryGracefulStop already called via registration; wait for exit
            if (!process.HasExited)
            {
                process.WaitForExit(3000);
            }
        }

        return process.ExitCode;
    }

    private static void TryGracefulStop(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.StandardInput.WriteLine("q");
            process.StandardInput.Flush();
        }
        catch
        {
            // ignore and fall back to kill
        }

        if (!process.WaitForExit(3000) && !process.HasExited)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
