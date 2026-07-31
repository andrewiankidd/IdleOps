using System.Drawing.Imaging;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Windows;
using waitfr.Cli;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

[assembly: SupportedOSPlatform("windows10.0.22621.0")]

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
        var showHelp = options.ShowHelp;

        if (showHelp || string.IsNullOrWhiteSpace(window))
        {
            Console.WriteLine("""
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
            return showHelp ? 0 : 1;
        }

        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        Console.Error.WriteLine($"[waitfr] Waiting for window '{window}'{(text != null ? $" with text '{text}'" : "")}{(gone ? " to disappear" : "")} (timeout {timeout:0.#}s)...");

        while (DateTime.UtcNow < deadline)
        {
            var match = WindowMatcher.FindWindow(window);

            if (gone)
            {
                if (match is null)
                {
                    Console.Error.WriteLine("[waitfr] Window gone.");
                    return 0;
                }
            }
            else if (match is not null)
            {
                if (text is null)
                {
                    Console.Error.WriteLine($"[waitfr] Window '{match.Title}' found.");
                    return 0;
                }

                // OCR check
                if (await WindowContainsTextAsync(match.Handle, text))
                {
                    Console.Error.WriteLine($"[waitfr] Text '{text}' found in '{match.Title}'.");
                    return 0;
                }
            }

            await Task.Delay(text is null ? 250 : 1000);
        }

        Console.Error.WriteLine($"[waitfr] Timed out after {timeout:0.#}s.");
        return 1;
    }

    private static async Task<bool> WindowContainsTextAsync(IntPtr handle, string searchText)
    {
        try
        {
            using var bitmap = WindowCapture.CaptureWindow(handle);
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            var stream = new InMemoryRandomAccessStream();
            await ms.CopyToAsync(stream.AsStreamForWrite());
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine is null) return false;

            var result = await engine.RecognizeAsync(softwareBitmap);
            return result.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
