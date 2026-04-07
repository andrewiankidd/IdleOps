using System.Reflection;
using IdleOps.Shared.Cli;

namespace scrcap.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        string? window = null;
        string output = "screenshot.png";
        var showHelp = false;
        var showVersion = false;

        new ArgParser(args)
            .On("-w", "--window", v => window = v)
            .On("-o", "--output", v => output = v)
            .Flag("-h", "--help", () => showHelp = true)
            .Flag("-v", "--version", () => showVersion = true)
            .Parse();

        return new Options(window, output, showHelp, showVersion);
    }

    public static string VersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
}
