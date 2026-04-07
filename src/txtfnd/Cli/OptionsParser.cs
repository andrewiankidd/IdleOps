using System.Reflection;
using IdleOps.Shared.Cli;

namespace txtfnd.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        string? window = null;
        string? text = null;
        var showHelp = false;
        var showVersion = false;

        new ArgParser(args)
            .On("-w", "--window", v => window = v)
            .On("-t", "--text", v => text = v)
            .Flag("-h", "--help", () => showHelp = true)
            .Flag("-v", "--version", () => showVersion = true)
            .Parse();

        return new Options(window, text, showHelp, showVersion);
    }

    public static string VersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
}
