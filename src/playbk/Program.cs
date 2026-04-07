using playbk.Cli;
using playbk.Execution;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;

namespace playbk;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Options options;
        try
        {
            options = OptionsParser.Parse(args);
        }
        catch (Exception parseError)
        {
            ConsoleLogger.Error(parseError.Message);
            HelpPrinter.Print(HelpFactory.BuildHelp());
            return 1;
        }

        var help = HelpFactory.BuildHelp();
        if (options.ShowHelp)
        {
            HelpPrinter.Print(help);
            return 0;
        }

        if (options.ShowVersion)
        {
            HelpPrinter.PrintVersion(help.Name, OptionsParser.VersionText);
            return 0;
        }

        var inputs = OptionsParser.ResolveInputFiles(options.InputPatterns, Directory.GetCurrentDirectory());
        if (inputs.Count == 0)
        {
            ConsoleLogger.Error("No input files found.");
            return 1;
        }

        ConsoleLogger.Info($"Discovered {inputs.Count} script(s). Output -> {options.OutputDirectory}");

        PrependPath(AppContext.BaseDirectory);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var captureSeconds = ResolveCaptureSeconds();
        using var runner = new ScriptRunner(Directory.GetCurrentDirectory(), options.OutputDirectory, captureSeconds);
        foreach (var scriptPath in inputs)
        {
            ConsoleLogger.Info($"Running script: {scriptPath}");
            try
            {
                await runner.RunAsync(scriptPath, cts.Token);
            }
            catch (OperationCanceledException)
            {
                ConsoleLogger.Warn("Cancelled.");
                return 1;
            }
        }

        await runner.DrainBackgroundTasksAsync();
        ConsoleLogger.Info("Completed all scripts.");
        return 0;
    }

    private static int ResolveCaptureSeconds()
    {
        var env = Environment.GetEnvironmentVariable("PLAYBK_CAPTURE_TIMER");
        if (int.TryParse(env, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return 10;
    }

    private static void PrependPath(string pathToAdd)
    {
        if (string.IsNullOrWhiteSpace(pathToAdd))
        {
            return;
        }

        var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!current.Split(Path.PathSeparator).Any(p => string.Equals(p, pathToAdd, StringComparison.OrdinalIgnoreCase)))
        {
            Environment.SetEnvironmentVariable("PATH", $"{pathToAdd}{Path.PathSeparator}{current}");
        }
    }
}
