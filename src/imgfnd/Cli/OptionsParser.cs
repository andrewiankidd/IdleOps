using IdleOps.Shared.Cli;

namespace imgfnd.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-w", "--window", v => opts = opts with { Window = v })
            .On("-i", "--image", v => opts = opts with { ImagePath = v })
            .On("--threshold", v =>
            {
                if (!double.TryParse(v, out var threshold)) throw new ArgumentException("Invalid threshold.");
                opts = opts with { Threshold = threshold };
            })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Flag("-v", "--version", () => opts = opts with { ShowVersion = true })
            .Parse();

        return opts;
    }
}
