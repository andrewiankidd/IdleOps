using IdleOps.Shared.Cli;

namespace waitfr.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-w", "--window", v => opts = opts with { Window = v })
            .On("-t", "--text", v => opts = opts with { Text = v })
            .On("--timeout", v =>
            {
                if (!double.TryParse(v, out var timeout)) throw new ArgumentException("Invalid timeout.");
                opts = opts with { Timeout = timeout };
            })
            .Flag("--gone", () => opts = opts with { Gone = true })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Flag("--version", () => opts = opts with { ShowVersion = true })
            .Parse();

        return opts;
    }
}
