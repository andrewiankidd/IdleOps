using System.Reflection;
using IdleOps.Shared.Cli;

namespace outcap.Cli;

public static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var mergedOutput = "outcap.mp4";
        var videoOutput = "outcap-video.mp4";
        var audioOutput = "outcap-audio.wav";
        double? globalTimer = null;
        double? globalDelay = null;
        double? videoTimer = null;
        double? videoDelay = null;
        string? windowTitle = null;
        double? audioTimer = null;
        double? audioDelay = null;
        var keepRaw = false;
        var showHelp = false;
        var showVersion = false;

        new ArgParser(args)
            .On("-o", "--output", v => mergedOutput = v)
            .On("-t", "--timer", v => globalTimer = ParsePositive(v, "Timer must be a positive number (seconds)."))
            .On("--duration", v => globalTimer = ParsePositive(v, "Timer must be a positive number (seconds)."))
            .On("-d", "--delay", v => globalDelay = ParseNonNegative(v, "Delay must be zero or a positive number (seconds)."))
            .On("-w", "--window", v => windowTitle = v)
            .On("--vid-window", v => windowTitle = v)
            .On("--vid-output", v => videoOutput = v)
            .On("--vid-delay", v => videoDelay = ParseNonNegative(v, "Video delay must be zero or a positive number."))
            .On("--vid-timer", v => videoTimer = ParsePositive(v, "Video timer must be a positive number."))
            .On("--aud-output", v => audioOutput = v)
            .On("--aud-delay", v => audioDelay = ParseNonNegative(v, "Audio delay must be zero or a positive number."))
            .On("--aud-timer", v => audioTimer = ParsePositive(v, "Audio timer must be a positive number."))
            .Flag("--keep-raw", () => keepRaw = true)
            .Flag("-h", "--help", () => showHelp = true)
            .Flag("-v", "--version", () => showVersion = true)
            .Flag("--v", () => showVersion = true)
            .Parse();

        var video = new VideoOptions(
            Path.GetFullPath(videoOutput), windowTitle,
            videoDelay ?? globalDelay, videoTimer ?? globalTimer);

        var audio = new AudioOptions(
            Path.GetFullPath(audioOutput),
            audioDelay ?? globalDelay, audioTimer ?? globalTimer);

        return new Options(
            Path.GetFullPath(mergedOutput), showHelp, showVersion, keepRaw,
            globalTimer, globalDelay, video, audio);
    }

    public static string VersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    private static double ParsePositive(string value, string message)
    {
        if (!double.TryParse(value, out var parsed) || parsed <= 0)
            throw new ArgumentException(message);
        return parsed;
    }

    private static double ParseNonNegative(string value, string message)
    {
        if (!double.TryParse(value, out var parsed) || parsed < 0)
            throw new ArgumentException(message);
        return parsed;
    }
}
