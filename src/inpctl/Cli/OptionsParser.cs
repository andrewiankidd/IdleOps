using IdleOps.Shared.Cli;

namespace inpctl.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-w", "--window", v => opts = opts with { Window = v })
            .On("--keyboard", v => opts = opts with { Keyboard = v })
            .On("--type", v => opts = opts with { Type = v })
            .On("--leftmouse", v => opts = opts with { LeftMouse = v })
            .On("--rightmouse", v => opts = opts with { RightMouse = v })
            .On("--middlemouse", v => opts = opts with { MiddleMouse = v })
            .On("--pid", v =>
            {
                if (!int.TryParse(v, out var pid)) throw new ArgumentException("Invalid pid.");
                opts = opts with { Pid = pid };
            })
            .On("--resize", v => opts = opts with { Resize = v })
            .On("--move", v => opts = opts with { Move = v })
            .On("--hold", v => opts = opts with { Hold = v })
            .On("--interval", v =>
            {
                if (!int.TryParse(v, out var ms)) throw new ArgumentException("Invalid interval.");
                opts = opts with { Interval = ms };
            })
            .On("--duration", v =>
            {
                if (!double.TryParse(v, out var seconds)) throw new ArgumentException("Invalid duration.");
                opts = opts with { Duration = seconds };
            })
            .On("--method", v => opts = opts with { Method = ParseMethod(v) })
            .Flag("--version", () => opts = opts with { ShowVersion = true })
            .Flag("--ctrlc", () => opts = opts with { SendCtrlC = true })
            .Flag("--move-cursor", () => opts = opts with { MoveCursor = true })
            .Flag("--background", () => opts = opts with { Background = true })
            .Flag("--maximize", () => opts = opts with { Maximize = true })
            .Flag("--minimize", () => opts = opts with { Minimize = true })
            .Flag("--restore", () => opts = opts with { Restore = true })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Parse();

        return opts;
    }

    internal static InputMethod ParseMethod(string value) => value.ToLowerInvariant() switch
    {
        "foreground" or "fg" => InputMethod.Foreground,
        "background" or "bg" => InputMethod.Background,
        _ => throw new ArgumentException($"Invalid --method '{value}' (use foreground|background)."),
    };
}
