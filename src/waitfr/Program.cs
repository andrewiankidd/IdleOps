using IdleOps.Shared.Capture;
using IdleOps.Shared.Ocr;
using IdleOps.Shared.Windowing;
using waitfr.Cli;
#if WINDOWS
using IdleOps.Shared.Win;
#endif

namespace waitfr;

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
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var window = options.Window;
        var text = options.Text;
        var timeout = options.Timeout;
        var gone = options.Gone;

        if (options.ShowVersion)
        {
            IdleOps.Shared.Cli.HelpPrinter.PrintVersion("waitfr", IdleOps.Shared.Cli.BuildInfo.Version);
            return 0;
        }

        if (options.ShowHelp || string.IsNullOrWhiteSpace(window))
        {
            IdleOps.Shared.Cli.HelpPrinter.PrintRaw("waitfr", """
                Usage: waitfr --window "<title>" [--text "<search>"] [--timeout <seconds>] [--gone]

                Wait for a window to appear (or disappear with --gone).
                Optionally wait for specific text to appear via OCR.

                Options:
                  -w, --window    Window title pattern (supports * wildcards)
                  -t, --text      Text to wait for via OCR (optional)
                  --timeout       Seconds to wait before failing (default: 10)
                  --gone          Wait for window to disappear instead
                  -h, --help      Show help

                Exit codes:
                  0 = condition met
                  1 = timeout or error
                """);
            return options.ShowHelp ? 0 : 1;
        }

        var locator = WindowLocatorFactory.Create();
        if (locator is null)
        {
            Console.Error.WriteLine("[waitfr] no window locator for this OS (supported: Windows, Linux/X11).");
            return 1;
        }

        // OCR is only needed when waiting for text — build the finder lazily.
        ImageTextFinder? finder = null;
        if (text is not null && !gone)
        {
            var capturer = ScreenCapturerFactory.Create();
            if (capturer is null)
            {
                Console.Error.WriteLine("[waitfr] no screen capturer for this OS.");
                return 1;
            }
#if WINDOWS
            ITextRecognizer recognizer = new WinRtTextRecognizer();
#else
            ITextRecognizer recognizer = new TesseractTextRecognizer();
#endif
            finder = new ImageTextFinder(capturer, recognizer);
        }

        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        Console.Error.WriteLine($"[waitfr] Waiting for window '{window}'{(text != null ? $" with text '{text}'" : "")}{(gone ? " to disappear" : "")} (timeout {timeout:0.#}s)...");

        while (DateTime.UtcNow < deadline)
        {
            var exists = locator.Exists(window);

            if (gone)
            {
                if (!exists)
                {
                    Console.Error.WriteLine("[waitfr] Window gone.");
                    return 0;
                }
            }
            else if (exists)
            {
                if (text is null)
                {
                    Console.Error.WriteLine($"[waitfr] Window '{locator.ResolveTitle(window) ?? window}' found.");
                    return 0;
                }

                if (await finder!.FindAsync(window, text) is not null)
                {
                    Console.Error.WriteLine($"[waitfr] Text '{text}' found in '{window}'.");
                    return 0;
                }
            }

            await Task.Delay(text is null ? 250 : 1000);
        }

        Console.Error.WriteLine($"[waitfr] Timed out after {timeout:0.#}s.");
        return 1;
    }
}
