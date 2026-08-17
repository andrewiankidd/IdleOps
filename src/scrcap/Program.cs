using IdleOps.Shared.Capture;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;
using scrcap.Cli;

namespace scrcap;

internal static class Program
{
    private static int Main(string[] args)
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

        if (string.IsNullOrWhiteSpace(options.Window))
        {
            ConsoleLogger.Error("--window is required (use 'screen' for the whole display).");
            return 1;
        }

        var capturer = ScreenCapturerFactory.Create();
        if (capturer is null)
        {
            ConsoleLogger.Error("no screen capturer for this OS (supported: Windows, Linux/X11, macOS).");
            return 1;
        }

        try
        {
            var outputPath = Path.GetFullPath(options.Output);
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var outcome = capturer.Capture(options.Window, outputPath);
            if (!outcome.Ok)
            {
                return 1;
            }

            Console.Error.WriteLine($"[scrcap] Saved {outcome.Width}x{outcome.Height} screenshot ({capturer.Name}).");
            Console.WriteLine(outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Capture failed: {ex.Message}");
            return 1;
        }
    }
}
