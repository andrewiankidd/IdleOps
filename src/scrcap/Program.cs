using System.Drawing.Imaging;
using System.Runtime.Versioning;
using IdleOps.Shared.Cli;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windows;
using scrcap.Cli;

[assembly: SupportedOSPlatform("windows")]

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

        if (!PlatformSupport.EnsureWindows("scrcap")) return 1;

        if (string.IsNullOrWhiteSpace(options.Window))
        {
            ConsoleLogger.Error("--window is required.");
            return 1;
        }

        var match = WindowMatcher.FindWindow(options.Window, preferNewest: true);
        if (match is null)
        {
            ConsoleLogger.Error($"Window '{options.Window}' not found.");
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

            Console.Error.WriteLine($"[scrcap] Capturing window '{match.Title}' -> {outputPath}");

            using var bitmap = WindowCapture.CaptureWindow(match.Handle);
            var format = GetImageFormat(outputPath);
            bitmap.Save(outputPath, format);

            Console.Error.WriteLine($"[scrcap] Saved {bitmap.Width}x{bitmap.Height} screenshot.");
            Console.WriteLine(outputPath);
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Capture failed: {ex.Message}");
            return 1;
        }
    }

    private static ImageFormat GetImageFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            ".tiff" or ".tif" => ImageFormat.Tiff,
            _ => ImageFormat.Png
        };
    }
}
