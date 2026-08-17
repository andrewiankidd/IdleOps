using IdleOps.Shared.Capture;
using imgfnd.Matching;

namespace imgfnd;

internal static class Program
{
    private static int Main(string[] args)
    {
        imgfnd.Cli.Options options;
        try
        {
            options = imgfnd.Cli.OptionsParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var window = options.Window;
        var imagePath = options.ImagePath;
        var threshold = options.Threshold;
        var showHelp = options.ShowHelp;
        var showVersion = options.ShowVersion;

        if (showHelp || (string.IsNullOrWhiteSpace(window) && !showVersion) || (string.IsNullOrWhiteSpace(imagePath) && !showVersion))
        {
            Console.WriteLine("""
                Usage: imgfnd --window "<title>" --image <reference.png> [--threshold 0.8]

                Find a reference image within a window screenshot and return its center coordinates.

                Options:
                  -w, --window      Window title pattern (supports * wildcards; 'screen' = whole display)
                  -i, --image       Path to reference image (PNG, JPG, BMP)
                  --threshold       Match confidence threshold 0.0-1.0 (default: 0.8)
                  -h, --help        Show help
                  -v, --version     Show version

                Output: x,y coordinates on stdout (center of matched region)
                Exit code: 0 = found, 1 = not found or error
                """);
            return showHelp ? 0 : 1;
        }

        if (showVersion)
        {
            Console.WriteLine($"imgfnd {typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0"}");
            return 0;
        }

        if (!File.Exists(imagePath))
        {
            Console.Error.WriteLine($"[imgfnd] Reference image not found: {imagePath}");
            return 1;
        }

        var capturer = ScreenCapturerFactory.Create();
        if (capturer is null)
        {
            Console.Error.WriteLine("[imgfnd] no screen capturer for this OS (supported: Windows, Linux/X11, macOS).");
            return 1;
        }

        var tempPng = Path.Combine(Path.GetTempPath(), $"imgfnd-{Guid.NewGuid():N}.png");
        try
        {
            Console.Error.WriteLine($"[imgfnd] Capturing window '{window}', matching against '{imagePath}'...");
            var outcome = capturer.Capture(window!, tempPng);
            if (!outcome.Ok)
            {
                return 1; // the capturer logs the reason (window not found, tool missing, ...)
            }

            var screenshot = ImageLoader.Load(tempPng);
            var template = ImageLoader.Load(Path.GetFullPath(imagePath!));

            if (template.Width > screenshot.Width || template.Height > screenshot.Height)
            {
                Console.Error.WriteLine("[imgfnd] Reference image is larger than the window screenshot.");
                return 1;
            }

            var match = TemplateMatcher.Match(screenshot, template);

            if (match.Confidence < threshold)
            {
                Console.Error.WriteLine($"[imgfnd] No match found (best confidence: {match.Confidence:0.###}, threshold: {threshold:0.###}).");
                return 1;
            }

            var centerX = match.X + template.Width / 2;
            var centerY = match.Y + template.Height / 2;

            Console.Error.WriteLine($"[imgfnd] Match found at ({match.X},{match.Y}) confidence {match.Confidence:0.###}");
            Console.WriteLine($"{centerX},{centerY}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[imgfnd] Failed: {ex.Message}");
            return 1;
        }
        finally
        {
            try { if (File.Exists(tempPng)) File.Delete(tempPng); } catch { /* best-effort */ }
        }
    }
}
