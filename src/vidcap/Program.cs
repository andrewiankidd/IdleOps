using vidcap.Cli;
using vidcap.Video;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;

namespace vidcap;

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

        var osDescription = HostInfo.Describe(HostInfo.Detect());
        ConsoleLogger.Info($"Starting video capture on {osDescription} -> {options.OutputPath}");

        var capturer = VideoCapturerFactory.Create(osDescription);
        if (capturer is null)
        {
            ConsoleLogger.Error($"No video capturer available for {osDescription}.");
            return 1;
        }

        if (options.DelaySeconds is { } delaySeconds && delaySeconds > 0)
        {
            ConsoleLogger.Info($"Delaying start by {delaySeconds:0.##}s...");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CancellationToken.None);
        }

        using var cts = new CancellationTokenSource();
        if (options.TimerSeconds is { } timerSeconds && timerSeconds > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(timerSeconds));
        }
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var exitCode = await capturer.CaptureAsync(options.OutputPath, options.WindowTitle, cts.Token);
            return exitCode;
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Error("Capture cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Capture failed: {ex.Message}");
            return 1;
        }
    }
}
