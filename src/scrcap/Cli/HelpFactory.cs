using IdleOps.Shared.Cli;

namespace scrcap.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "scrcap",
            "Capture a screenshot of a window and save to file.",
            new[]
            {
                "scrcap --window \"Notepad*\" -o screenshot.png",
                "scrcap -w \"*Chrome*\" -o captures/browser.png",
                "scrcap -w \"My App*\" -o output.jpg"
            },
            new[]
            {
                "-w, --window      Window title pattern (supports * wildcards)",
                "-o, --output      Output file path (default: screenshot.png)",
                "-h, --help        Show help",
                "-v, --version     Show version"
            }
        );
    }
}
