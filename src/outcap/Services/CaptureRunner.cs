using System.Diagnostics;
using outcap.Cli;
using IdleOps.Shared.Capture;
using IdleOps.Shared.Logging;
using audcap.Services;
using vidcap.Services;

namespace outcap.Services;

public sealed class CaptureRunner
{
    private readonly Options _options;

    public CaptureRunner(Options options)
    {
        _options = options;
    }

    public async Task<int> RunAsync(CancellationToken token)
    {
        var audioTask = AudcapService.CaptureAsync(
            _options.Audio.OutputPath,
            delaySeconds: _options.Audio.DelaySeconds,
            timerSeconds: _options.Audio.TimerSeconds,
            token: token);

        var videoTask = VidcapService.CaptureAsync(
            _options.Video.OutputPath,
            windowTitle: _options.Video.WindowTitle,
            delaySeconds: _options.Video.DelaySeconds,
            timerSeconds: _options.Video.TimerSeconds,
            token: token);

        await Task.WhenAll(audioTask, videoTask);

        var audio = audioTask.Result;
        var video = videoTask.Result;

        var hadErrors = audio.ExitCode != 0 || video.ExitCode != 0;
        if (hadErrors)
        {
            ConsoleLogger.Warn($"Capture reported errors (audio exit {audio.ExitCode}, video exit {video.ExitCode}). Attempting merge if outputs exist...");
        }

        await MergeAsync(audio, video, token);
        return hadErrors ? 1 : 0;
    }

    private async Task MergeAsync(CaptureResult audio, CaptureResult video, CancellationToken token)
    {
        var hasAudio = HasNonEmptyFile(audio.OutputPath);
        var hasVideo = HasNonEmptyFile(video.OutputPath);

        if (!hasAudio && !hasVideo)
        {
            ConsoleLogger.Warn("Merge skipped: missing audio and video files.");
            return;
        }

        var mergedPath = _options.MergedOutput;
        Directory.CreateDirectory(Path.GetDirectoryName(mergedPath) ?? ".");

        var mergedOk = false;

        if (!hasAudio && hasVideo)
        {
            ConsoleLogger.Warn("Audio missing; copying video only to merged output.");
            File.Copy(video.OutputPath, mergedPath, overwrite: true);
            mergedOk = true;
        }
        else if (hasAudio && !hasVideo)
        {
            ConsoleLogger.Warn("Video missing; copying audio only to merged output.");
            File.Copy(audio.OutputPath, mergedPath, overwrite: true);
            mergedOk = true;
        }
        else
        {
            var offsetSeconds = (video.StartTimeUtc - audio.StartTimeUtc).TotalSeconds;
            var audioOffset = offsetSeconds > 0 ? offsetSeconds : 0;
            var videoOffset = offsetSeconds < 0 ? -offsetSeconds : 0;

            var ffmpegArgs = BuildFfmpegArgs(audio.OutputPath, video.OutputPath, mergedPath, audioOffset, videoOffset);

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    ConsoleLogger.Warn("Failed to start ffmpeg for merge.");
                }
                else
                {
                    var stderr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(token);

                    if (process.ExitCode != 0)
                    {
                        ConsoleLogger.Warn($"ffmpeg merge failed (exit {process.ExitCode}): {stderr}");
                    }
                    else
                    {
                        ConsoleLogger.Info($"Merged capture -> {mergedPath}");
                        mergedOk = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                ConsoleLogger.Warn("Merge cancelled.");
            }
            catch (Exception ex)
            {
                ConsoleLogger.Warn($"Merge failed: {ex.Message}");
            }
        }

        // Fallback: if merge didn't produce a usable file but video exists, copy video.
        if (!mergedOk && hasVideo)
        {
            ConsoleLogger.Warn("Merge incomplete; copying raw video to merged output.");
            File.Copy(video.OutputPath, mergedPath, overwrite: true);
            mergedOk = true;
        }

        if (!_options.KeepRaw)
        {
            TryDelete(audio.OutputPath);
            TryDelete(video.OutputPath);
        }
    }

    private static string BuildFfmpegArgs(string audioPath, string videoPath, string outputPath, double audioOffset, double videoOffset)
    {
        var audioInput = audioOffset > 0 ? $"-itsoffset {audioOffset:0.###} -i \"{audioPath}\"" : $"-i \"{audioPath}\"";
        var videoInput = videoOffset > 0 ? $"-itsoffset {videoOffset:0.###} -i \"{videoPath}\"" : $"-i \"{videoPath}\"";
        return $"-y -loglevel error {audioInput} {videoInput} -map 1:v:0 -map 0:a:0 -c:v copy -c:a aac -shortest \"{outputPath}\"";
    }

    private static bool HasNonEmptyFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            ConsoleLogger.Warn($"Failed to delete '{path}': {ex.Message}");
        }
    }
}
