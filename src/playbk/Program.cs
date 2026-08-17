using playbk.Ai;
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

        PrependPath(AppContext.BaseDirectory);

        var profile = DeviceProfile.Resolve(options.Profile);
        if (profile is null)
        {
            ConsoleLogger.Error($"Unknown --profile '{options.Profile}'. Available: {string.Join(", ", DeviceProfile.All.Select(p => p.Name))}.");
            return 1;
        }

        using var genCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; genCts.Cancel(); };

        // AI generator mode: turn a natural-language goal into a playbook, print it
        // for review, then run it (unless --dry-run).
        if (!string.IsNullOrWhiteSpace(options.Goal))
        {
            return await GenerateAsync(options, profile, genCts.Token);
        }

        var inputs = OptionsParser.ResolveInputFiles(options.InputPatterns, Directory.GetCurrentDirectory());
        if (inputs.Count == 0)
        {
            ConsoleLogger.Error("No input files found.");
            return 1;
        }

        ConsoleLogger.Info($"Discovered {inputs.Count} script(s). Output -> {options.OutputDirectory}");

        var captureSeconds = ResolveCaptureSeconds();
        using var runner = new ScriptRunner(Directory.GetCurrentDirectory(), options.OutputDirectory, captureSeconds, profile);
        var exitCode = 0;
        foreach (var scriptPath in inputs)
        {
            ConsoleLogger.Info($"Running script: {scriptPath} (profile: {profile.Name})");
            try
            {
                var code = await runner.RunAsync(scriptPath, genCts.Token);
                if (code != 0)
                {
                    ConsoleLogger.Error($"Script failed (exit {code}): {scriptPath}");
                    exitCode = code;
                }
            }
            catch (OperationCanceledException)
            {
                ConsoleLogger.Warn("Cancelled.");
                return 1;
            }
        }

        await runner.DrainBackgroundTasksAsync();
        ConsoleLogger.Info(exitCode == 0 ? "Completed all scripts." : "Finished with failures.");
        return exitCode;
    }

    // AI generator mode: goal -> playbook YAML (via the resolved LLM registry) ->
    // print for review -> run it, unless --dry-run.
    private static async Task<int> GenerateAsync(Options options, DeviceProfile profile, CancellationToken token)
    {
        ConsoleLogger.Info($"Planning a playbook for: {options.Goal} (profile: {profile.Name})");

        string raw;
        string? backend;
        IReadOnlyList<string> genErrors;
        try
        {
            (raw, backend, genErrors) = await PlaybookPlanner.GenerateAsync(options.Goal!, token);
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Warn("Cancelled.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            ConsoleLogger.Error($"AI planning produced nothing. {string.Join(" · ", genErrors)}");
            return 1;
        }

        var (script, errors) = PlaybookPlanner.ParseAndValidate(raw);
        if (script is null)
        {
            ConsoleLogger.Error($"Generated playbook is invalid: {string.Join(" · ", errors)}");
            Console.Error.WriteLine("--- raw model output ---");
            Console.Error.WriteLine(raw);
            return 1;
        }

        var yaml = PlaybookPlanner.ExtractYaml(raw);
        if (backend is not null) ConsoleLogger.Info($"Planned via {backend}:");
        Console.WriteLine();
        Console.WriteLine(yaml);
        Console.WriteLine();

        // A plan that parses can still be invalid for the target transport (e.g. a
        // UIA step for an off-box profile). Surface that with the plan, before running.
        var violations = RunbookValidator.Validate(script, profile);
        if (violations.Count > 0)
        {
            ConsoleLogger.Error(RunbookValidator.Format(violations, profile));
            return 1;
        }

        if (options.DryRun)
        {
            ConsoleLogger.Info("--dry-run: not executing. Save the YAML above to inputs/ to run it later.");
            return 0;
        }

        Directory.CreateDirectory(options.OutputDirectory);
        var generatedPath = Path.Combine(options.OutputDirectory, "generated.idleops.yaml");
        await File.WriteAllTextAsync(generatedPath, yaml, token);
        ConsoleLogger.Info($"Running generated playbook ({generatedPath})...");

        var captureSeconds = ResolveCaptureSeconds();
        using var runner = new ScriptRunner(Directory.GetCurrentDirectory(), options.OutputDirectory, captureSeconds, profile);
        try
        {
            var code = await runner.RunAsync(generatedPath, token);
            await runner.DrainBackgroundTasksAsync();
            return code;
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Warn("Cancelled.");
            return 1;
        }
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
