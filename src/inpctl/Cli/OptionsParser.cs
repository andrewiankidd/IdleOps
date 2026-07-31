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
}
