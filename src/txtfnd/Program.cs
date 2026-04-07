using txtfnd.Cli;
using txtfnd.Ocr;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Windows;

namespace txtfnd;

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

        if (string.IsNullOrWhiteSpace(options.Window))
        {
            ConsoleLogger.Error("--window is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(options.Text))
        {
            ConsoleLogger.Error("--text is required.");
            return 1;
        }

        var match = WindowMatcher.FindWindow(options.Window, preferNewest: true);
        if (match is null)
        {
            ConsoleLogger.Error($"Window '{options.Window}' not found.");
            return 1;
        }

        // Status messages go to stderr so stdout is clean for piping
        Console.Error.WriteLine($"[txtfnd] Capturing window '{match.Title}' for OCR...");

        try
        {
            using var bitmap = WindowCapture.CaptureWindow(match.Handle);
            var coords = await OcrService.FindTextAsync(bitmap, options.Text);

            if (coords is null)
            {
                Console.Error.WriteLine($"[txtfnd] Text '{options.Text}' not found in window '{match.Title}'.");

                // Debug: show what was recognized
                var all = await OcrService.RecognizeAllAsync(bitmap);
                if (all.Count > 0)
                {
                    Console.Error.WriteLine("[txtfnd] Recognized text:");
                    foreach (var r in all)
                    {
                        Console.Error.WriteLine($"[txtfnd]   \"{r.Text}\" at ({r.X},{r.Y}) {r.Width}x{r.Height}");
                    }
                }

                return 1;
            }

            // Output coordinates on stdout for piping to inpctl
            Console.WriteLine($"{coords.Value.x},{coords.Value.y}");
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"OCR failed: {ex.Message}");
            return 1;
        }
    }
}
