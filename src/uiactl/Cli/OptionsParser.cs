using IdleOps.Shared.Cli;

namespace uiactl.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-w", "--window", v => opts = opts with { Window = v })
            .On("--automation-id", v => opts = opts with { AutomationId = v })
            .On("--name", v => opts = opts with { Name = v })
            .On("--control-type", v => opts = opts with { ControlType = v })
            .On("--set-value", v => opts = opts with { SetValue = v })
            .On("--element-at", v => opts = opts with { ElementAt = v })
            .On("--max", v =>
            {
                if (!int.TryParse(v, out var max)) throw new ArgumentException("Invalid --max.");
                opts = opts with { Max = max };
            })
            .Flag("--get-value", () => opts = opts with { GetValue = true })
            .Flag("--invoke", () => opts = opts with { Invoke = true })
            .Flag("--toggle", () => opts = opts with { Toggle = true })
            .Flag("--expand", () => opts = opts with { Expand = true })
            .Flag("--collapse", () => opts = opts with { Collapse = true })
            .Flag("--select", () => opts = opts with { Select = true })
            .Flag("--dump", () => opts = opts with { Dump = true })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Flag("--version", () => opts = opts with { ShowVersion = true })
            .Parse();

        return opts;
    }
}
