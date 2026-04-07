using System.Diagnostics;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Xunit;

namespace audcap.Tests;

public class RecordingTests
{
    [Fact]
    public async Task CapturesSystemAudioForFiveSeconds()
    {
        if (!OperatingSystem.IsWindows())
        {
            // WASAPI loopback is Windows-only; skip elsewhere.
            return;
        }

        var audcapExe = FindAudcapExe();
        Assert.False(string.IsNullOrEmpty(audcapExe));

        var tempDir = Path.Combine(Path.GetTempPath(), "audcap-tests");
        Directory.CreateDirectory(tempDir);
        var outputFile = Path.Combine(tempDir, $"capture-{Guid.NewGuid():N}.wav");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = audcapExe!,
                Arguments = $"--output \"{outputFile}\" --timer 6",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();

        await PlayToneAsync(TimeSpan.FromSeconds(5), 440f, 0.2f);

        var exited = process.WaitForExit(8000);
        if (!exited)
        {
            process.Kill(true);
        }

        Assert.True(File.Exists(outputFile));

        using var reader = new AudioFileReader(outputFile);
        var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        var read = reader.Read(buffer, 0, buffer.Length);
        Assert.True(read > 0, "No samples read from capture.");
        Assert.True(buffer.Any(sample => Math.Abs(sample) > 0.001f), "Capture appears silent.");
    }

    [Fact]
    public async Task TimerStopsAroundRequestedDuration()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var audcapExe = FindAudcapExe();
        Assert.False(string.IsNullOrEmpty(audcapExe));

        var timerSeconds = 2.0;
        var outputFile = CreateTempOutput();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = audcapExe!,
                Arguments = $"--output \"{outputFile}\" --timer {timerSeconds}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var sw = Stopwatch.StartNew();
        process.Start();
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();

        await PlayToneAsync(TimeSpan.FromSeconds(5), 440f, 0.2f);

        var exited = process.WaitForExit(10000);
        if (!exited)
        {
            process.Kill(true);
        }
        sw.Stop();

        Assert.InRange(sw.Elapsed.TotalSeconds, timerSeconds, timerSeconds + 6);
        Assert.True(File.Exists(outputFile));

        var duration = GetAudioDuration(outputFile);
        Assert.InRange(duration.TotalSeconds, timerSeconds * 0.6, timerSeconds + 1);
    }

    [Fact]
    public async Task DelayIsRespectedBeforeCaptureStarts()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var audcapExe = FindAudcapExe();
        Assert.False(string.IsNullOrEmpty(audcapExe));

        var delaySeconds = 2.0;
        var timerSeconds = 2.0;
        var outputFile = CreateTempOutput();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = audcapExe!,
                Arguments = $"--output \"{outputFile}\" --delay {delaySeconds} --timer {timerSeconds}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var sw = Stopwatch.StartNew();
        process.Start();
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();

        await PlayToneAsync(TimeSpan.FromSeconds(6), 440f, 0.2f);

        var exited = process.WaitForExit(12000);
        if (!exited)
        {
            process.Kill(true);
        }
        sw.Stop();

        // Expect roughly delay+timer, with some tolerance for startup/teardown overhead.
        Assert.InRange(sw.Elapsed.TotalSeconds, delaySeconds + timerSeconds, delaySeconds + timerSeconds + 6);
        Assert.True(File.Exists(outputFile));

        var duration = GetAudioDuration(outputFile);
        Assert.InRange(duration.TotalSeconds, timerSeconds * 0.6, timerSeconds + 1);
    }

    private static string? FindAudcapExe()
    {
        var binRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "audcap", "bin"));
        return Directory.Exists(binRoot)
            ? Directory.EnumerateFiles(binRoot, "audcap.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private static async Task PlayToneAsync(TimeSpan duration, float frequency, float gain)
    {
        var signal = new SignalGenerator
        {
            Gain = gain,
            Frequency = frequency,
            Type = SignalGeneratorType.Sin
        };

        var limited = new OffsetSampleProvider(signal) { Take = duration };
        using var waveOut = new WaveOutEvent();
        waveOut.Init(new SampleToWaveProvider(limited));
        waveOut.Play();
        await Task.Delay(duration + TimeSpan.FromMilliseconds(200));
        waveOut.Stop();
    }

    private static string CreateTempOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "audcap-tests");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, $"capture-{Guid.NewGuid():N}.wav");
    }

    private static TimeSpan GetAudioDuration(string path)
    {
        using var reader = new AudioFileReader(path);
        return reader.TotalTime;
    }
}
