using IdleOps.Shared.Cli;

namespace audcap.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "audcap",
            "Cross-platform system audio capture CLI targeting net10.0.",
            new[]
            {
                "dotnet run --project src/audcap -- -o mycap.wav",
                "dotnet run --project src/audcap -- -d 2 -t 5 -o mycap.wav",
                "dotnet run --project src/audcap -- --help",
                "dotnet run --project src/audcap -- --version"
            },
            new[]
            {
                "-o, --output      Output file path (default: cap.wav)",
                "-d, --delay       Delay before starting capture (seconds)",
                "-t, --timer       Limit capture duration (seconds) (alias: --duration)",
                "-h, --help        Show help",
                "-v, --v, --version Show version"
            }
        );
    }
}
