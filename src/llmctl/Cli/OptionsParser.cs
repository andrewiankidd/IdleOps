using IdleOps.Shared.Cli;

namespace llmctl.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-g", "--goal", v => opts = opts with { Goal = v })
            .On("-i", "--image", v => opts = opts with { Image = v })
            .On("-s", "--system", v => opts = opts with { System = v })
            .On("--endpoint", v => opts = opts with { Endpoint = v })
            .On("--model", v => opts = opts with { Model = v })
            .On("--api-key", v => opts = opts with { ApiKey = v })
            .On("--temperature", v =>
            {
                if (!double.TryParse(v, out var t)) throw new ArgumentException("Invalid temperature.");
                opts = opts with { Temperature = t };
            })
            .Flag("--list", () => opts = opts with { List = true })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Parse();

        return opts;
    }
}
