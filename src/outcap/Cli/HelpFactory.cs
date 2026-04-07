using IdleOps.Shared.Cli;

namespace outcap.Cli;

internal static class HelpFactory
{
    public static HelpContent BuildHelp()
    {
        return new HelpContent(
            "outcap",
            "Capture audio and video together, then merge into a single mp4.",
            new[]
            {
                "outcap -t 10",
                "outcap --output merged.mp4 --window \"My App\" --keep-raw",
                "outcap --vid-timer 10 --aud-timer 12 -o demo.mp4"
            },
            new[]
            {
                "-o, --output <path>     Merged output mp4 (default: outcap.mp4)",
                "--keep-raw              Keep raw audio/video files (default: delete)",
                "-t, --timer <sec>       Timer/duration for both captures",
                "-d, --delay <sec>       Start delay for both captures",
                "-w, --window <title>    Video window title filter",
                "--vid-output <path>     Video output path (default: outcap-video.mp4)",
                "--vid-delay <sec>       Video start delay",
                "--vid-timer <sec>       Video timer/duration",
                "--vid-window <title>    Video window title (alias for -w)",
                "--aud-output <path>     Audio output path (default: outcap-audio.wav)",
                "--aud-delay <sec>       Audio start delay",
                "--aud-timer <sec>       Audio timer/duration",
                "-h, --help              Show help",
                "-v, --version           Show version"
            }
        );
    }
}
