using IdleOps.Shared.Cli;

namespace vidcap.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "vidcap",
            "Cross-platform screen capture CLI targeting net10.0.",
            new[]
            {
                "dotnet run --project src/vidcap -- -o mycap.mp4",
                "dotnet run --project src/vidcap -- -d 2 -t 5 -o clip.mp4",
                "dotnet run --project src/vidcap -- -w \"Untitled - Notepad\" -t 10",
                "dotnet run --project src/vidcap -- --help",
                "dotnet run --project src/vidcap -- --version"
            },
            new[]
            {
                "-o, --output   Output file path (default: cap.mp4)",
                "-d, --delay    Delay before starting capture (seconds)",
                "-t, --timer    Limit capture duration (seconds) (alias: --duration)",
                "-w, --window   Window title to pin capture (Windows only today)",
                "-h, --help     Show help",
                "-v, --v, --version Show version"
            }
        );
    }
}
