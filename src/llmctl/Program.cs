using IdleOps.Shared.Logging;
using llmctl.Cli;
using llmctl.Llm;

namespace llmctl;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Options options;
        try
        {
            options = OptionsParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            ConsoleLogger.Error($"[llmctl] {ex.Message}");
            return 1;
        }

        if (options.ShowHelp || !options.HasAction)
        {
            HelpFactory.PrintHelp();
            return options.HasAction ? 0 : 1;
        }

        if (options.List)
        {
            PrintRegistry();
            return 0;
        }

        string? imageBase64 = null;
        if (options.Image is not null)
        {
            if (!File.Exists(options.Image))
            {
                ConsoleLogger.Error($"[llmctl] image not found: {options.Image}");
                return 1;
            }
            imageBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(options.Image));
        }

        // Direct mode: an explicit endpoint bypasses the registry.
        if (options.Endpoint is not null)
        {
            var direct = new OnlineBackend(options.Endpoint, options.Model ?? "qwen2.5vl:7b", options.ApiKey, options.Temperature, "direct");
            return await RunAsync(direct, options, imageBase64);
        }

        // Registry mode: resolve the autotuned registry and try active candidates
        // in priority order, falling through on failure (shared with playbk).
        var result = await LlmRunner.CompleteAsync(
            options.System, options.Goal!, imageBase64, options.Temperature, CancellationToken.None,
            onAttempt: label => Console.Error.WriteLine($"[llmctl] using {label}"));

        if (result.Ok)
        {
            Console.WriteLine(result.Text);
            return 0;
        }

        ConsoleLogger.Error($"[llmctl] all backends failed — {string.Join(" · ", result.Errors)}");
        return 1;
    }

    private static async Task<int> RunAsync(IChatBackend backend, Options options, string? imageBase64)
    {
        try
        {
            var reply = await backend.CompleteAsync(options.System, options.Goal!, imageBase64, CancellationToken.None);
            Console.WriteLine(reply);
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"[llmctl] {ex.Message}");
            return 1;
        }
    }

    private static void PrintRegistry()
    {
        var caps = PlatformCaps.Detect();
        var config = LlmConfig.Load();
        Console.WriteLine($"Platform: embedded={caps.Embedded} gpu={caps.Gpu}");
        Console.WriteLine($"Registry ({(config.Auto ? "auto/seeded" : "user")}) — order is priority:");
        foreach (var i in config.Integrations)
        {
            var status =
                !i.Compatible(caps) ? "incompatible"
                : !i.Configured() ? "not configured"
                : i.Kind == "offline" && !EmbeddedBackend.IsAvailable ? "unavailable"
                : "READY";
            var what = i.Model ?? i.OfflineModel ?? i.Provider ?? "";
            Console.WriteLine($"  [{status,-14}] {i.Id,-14} {i.Kind,-8} {what}");
        }
        Console.WriteLine($"Config: {LlmConfig.ConfigPath()}");
    }
}
