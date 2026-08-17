using System.Reflection;
using IdleOps.Shared.Cli;
using Microsoft.Extensions.FileSystemGlobbing;

namespace playbk.Cli;

internal static class OptionsParser
{
    private static readonly string[] DefaultPatterns = { "inputs/*.idleops.yaml", "inputs/*.yaml" };

    public static Options Parse(string[] args)
    {
        var patterns = new List<string>();
        var outputDir = "outputs";
        var showHelp = false;
        var showVersion = false;
        string? goal = null;
        var dryRun = false;
        var profile = "local";

        new ArgParser(args)
            .On("-i", "--input", v => patterns.AddRange(v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .On("-o", "--output", v => outputDir = v)
            .On("-g", "--goal", v => goal = v)
            .On("-p", "--profile", v => profile = v)
            .Flag("--dry-run", () => dryRun = true)
            .Flag("-h", "--help", () => showHelp = true)
            .Flag("-v", "--version", () => showVersion = true)
            .Flag("--v", () => showVersion = true)
            .Parse();

        if (patterns.Count == 0)
            patterns.AddRange(DefaultPatterns);

        return new Options(patterns, Path.GetFullPath(outputDir), showHelp, showVersion, goal, dryRun, profile);
    }

    public static IReadOnlyList<string> ResolveInputFiles(IReadOnlyList<string> patterns, string baseDir)
    {
        var matcher = new Matcher();
        foreach (var pattern in patterns)
            matcher.AddInclude(pattern);

        return matcher.GetResultsInFullPath(baseDir)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string VersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
}
