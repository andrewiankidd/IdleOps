using IdleOps.Shared.Cli;

namespace playbk.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "playbk",
            "Execute IdleOps scripts to drive repeatable flows (desktop/browser).",
            new[]
            {
                "dotnet run --project src/playbk -- -i inputs/rickroll.idleops.yaml",
                "dotnet run --project src/playbk -- -i \"inputs/*.yaml\" -o ./outputs"
            },
            new[]
            {
                "-i, --input    Input script pattern(s) (comma-separated). Default: inputs/*.idleops.yaml, inputs/*.yaml",
                "-o, --output   Output directory for captures (default: outputs)",
                "-h, --help     Show help",
                "-v, --v, --version Show version"
            }
        );
    }
}
