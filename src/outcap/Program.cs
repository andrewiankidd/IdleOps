using outcap.Cli;
using outcap.Services;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;

namespace outcap;

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
            IdleOps.Shared.Cli.HelpPrinter.Print(HelpFactory.BuildHelp());
            return 1;
        }

        var help = HelpFactory.BuildHelp();
        if (options.ShowHelp)
        {
            IdleOps.Shared.Cli.HelpPrinter.Print(help);
            return 0;
        }

        if (options.ShowVersion)
        {
            IdleOps.Shared.Cli.HelpPrinter.PrintVersion(help.Name, OptionsParser.VersionText);
            return 0;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var runner = new CaptureRunner(options);
        try
        {
            return await runner.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            ConsoleLogger.Warn("Cancelled.");
            return 1;
        }
    }
}
