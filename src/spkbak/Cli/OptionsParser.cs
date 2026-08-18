using IdleOps.Shared.Cli;

namespace spkbak.Cli;

internal static class OptionsParser
{
    public static Options Parse(string[] args)
    {
        var opts = new Options();

        new ArgParser(args)
            .On("-t", "--text", v => opts = opts with { Text = v })
            .On("-f", "--file", v => opts = opts with { File = v })
            .On("-o", "--output", v => opts = opts with { Output = v })
            .On("--voice", v => opts = opts with { Voice = v })
            .Flag("--list", () => opts = opts with { List = true })
            .Flag("-h", "--help", () => opts = opts with { ShowHelp = true })
            .Flag("--version", () => opts = opts with { ShowVersion = true })
            .Parse();

        return opts;
    }
}
