using System.Reflection;
using IdleOps.Shared.Cli;

namespace vidcap.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var outputPath = "cap.mp4";
        var showHelp = false;
        var showVersion = false;
        double? delaySeconds = null;
        double? timerSeconds = null;
        string? windowTitle = null;

        new ArgParser(args)
            .On("-o", "--output", v => outputPath = v)
            .On("-t", "--timer", v => timerSeconds = ParsePositive(v, "Timer must be a positive number (seconds)."))
            .On("--duration", v => timerSeconds = ParsePositive(v, "Timer must be a positive number (seconds)."))
            .On("-d", "--delay", v => delaySeconds = ParseNonNegative(v, "Delay must be zero or a positive number (seconds)."))
            .On("-w", "--window", v => windowTitle = v)
            .Flag("-h", "--help", () => showHelp = true)
            .Flag("-v", "--version", () => showVersion = true)
            .Flag("--v", () => showVersion = true)
            .Parse();

        return new Options(Path.GetFullPath(outputPath), showHelp, showVersion, delaySeconds, timerSeconds, windowTitle);
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
