using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using IdleOps.Shared.Windows;
using OpenCvSharp;

[assembly: SupportedOSPlatform("windows")]

namespace imgfnd;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? window = null;
        string? imagePath = null;
        double threshold = 0.8;
        bool showHelp = false;
        bool showVersion = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-w":
                case "--window":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for window."); return 1; }
                    window = args[++i];
                    break;
                case "-i":
                case "--image":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for image."); return 1; }
                    imagePath = args[++i];
                    break;
                case "--threshold":
                    if (i + 1 >= args.Length || !double.TryParse(args[++i], out threshold)) { Console.Error.WriteLine("Invalid threshold."); return 1; }
                    break;
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "-v":
                case "--version":
                    showVersion = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return 1;
            }
        }

        if (showHelp || (string.IsNullOrWhiteSpace(window) && !showVersion) || (string.IsNullOrWhiteSpace(imagePath) && !showVersion))
        {
            Console.WriteLine("""
                Usage: imgfnd --window "<title>" --image <reference.png> [--threshold 0.8]

                Find a reference image within a window screenshot and return its center coordinates.

                Options:
                  -w, --window      Window title pattern (supports * wildcards)
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

        var match = WindowMatcher.FindWindow(window!, preferNewest: true);
        if (match is null)
        {
            Console.Error.WriteLine($"[imgfnd] Window '{window}' not found.");
            return 1;
        }

        Console.Error.WriteLine($"[imgfnd] Capturing window '{match.Title}', matching against '{imagePath}'...");

        try
        {
            using var bitmap = WindowCapture.CaptureWindow(match.Handle);
            using var screenshotMat = BitmapToMat(bitmap);
            using var templateMat = Cv2.ImRead(Path.GetFullPath(imagePath!), ImreadModes.Color);

            if (templateMat.Empty())
            {
                Console.Error.WriteLine("[imgfnd] Failed to load reference image.");
                return 1;
            }

            if (templateMat.Width > screenshotMat.Width || templateMat.Height > screenshotMat.Height)
            {
                Console.Error.WriteLine("[imgfnd] Reference image is larger than the window screenshot.");
                return 1;
            }

            using var result = new Mat();
            Cv2.MatchTemplate(screenshotMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal < threshold)
            {
                Console.Error.WriteLine($"[imgfnd] No match found (best confidence: {maxVal:0.###}, threshold: {threshold:0.###}).");
                return 1;
            }

            var centerX = maxLoc.X + templateMat.Width / 2;
            var centerY = maxLoc.Y + templateMat.Height / 2;

            Console.Error.WriteLine($"[imgfnd] Match found at ({maxLoc.X},{maxLoc.Y}) confidence {maxVal:0.###}");
            Console.WriteLine($"{centerX},{centerY}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[imgfnd] Failed: {ex.Message}");
            return 1;
        }
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        return Mat.FromImageData(ms.ToArray(), ImreadModes.Color);
    }
}
