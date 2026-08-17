using txtfnd.Cli;
using IdleOps.Shared.Capture;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Ocr;
#if WINDOWS
using IdleOps.Shared.Win;
#endif

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

        var capturer = ScreenCapturerFactory.Create();
        if (capturer is null)
        {
            ConsoleLogger.Error("no screen capturer for this OS (supported: Windows, Linux/X11, macOS).");
            return 1;
        }

        // WinRT OCR on Windows (zero-install, warm engine); Tesseract elsewhere.
#if WINDOWS
        ITextRecognizer recognizer = new WinRtTextRecognizer();
#else
        ITextRecognizer recognizer = new TesseractTextRecognizer();
#endif
        var finder = new ImageTextFinder(capturer, recognizer);

        Console.Error.WriteLine($"[txtfnd] Capturing window '{options.Window}' for OCR ({recognizer.Name})...");

        try
        {
            var words = await finder.RecognizeAllAsync(options.Window);
            var coords = TextLocator.Locate(words, options.Text);

            if (coords is null)
            {
                Console.Error.WriteLine($"[txtfnd] Text '{options.Text}' not found in window '{options.Window}'.");
                if (words.Count > 0)
                {
                    Console.Error.WriteLine("[txtfnd] Recognized text:");
                    foreach (var w in words)
                        Console.Error.WriteLine($"[txtfnd]   \"{w.Text}\" at ({w.X},{w.Y}) {w.Width}x{w.Height}");
                }
                return 1;
            }

            // Coordinates on stdout for piping to inpctl.
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
