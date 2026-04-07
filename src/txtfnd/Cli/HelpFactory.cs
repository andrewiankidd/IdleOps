using IdleOps.Shared.Cli;

namespace txtfnd.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "txtfnd",
            "Find text on screen via OCR and return its coordinates.",
            new[]
            {
                "txtfnd --window \"Notepad*\" --text \"File\"",
                "txtfnd --window \"*Chrome*\" --text \"Settings\"",
                "txtfnd -w \"Paint*\" -t \"Brushes\""
            },
            new[]
            {
                "-w, --window      Window title pattern (supports * wildcards)",
                "-t, --text        Text to search for (case-insensitive)",
                "-h, --help        Show help",
                "-v, --version     Show version"
            }
        );
    }
}
