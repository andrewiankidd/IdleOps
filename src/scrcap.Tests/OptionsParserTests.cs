using scrcap.Cli;
using Xunit;

namespace scrcap.Tests;

public class OptionsParserTests
{
    [Fact]
    public void ParsesWindowAndOutput()
    {
        var opts = OptionsParser.Parse(new[] { "-w", "Notepad*", "-o", "shot.png" });
        Assert.Equal("Notepad*", opts.Window);
        Assert.Equal("shot.png", opts.Output);
    }

    [Fact]
    public void DefaultOutput()
    {
        var opts = OptionsParser.Parse(new[] { "-w", "Test*" });
        Assert.Equal("screenshot.png", opts.Output);
    }

    [Fact]
    public void ParsesHelp()
    {
        var opts = OptionsParser.Parse(new[] { "--help" });
        Assert.True(opts.ShowHelp);
    }

    [Fact]
    public void UnknownArgThrows()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(new[] { "--badarg" }));
    }

    [Fact]
    public void MissingWindowValueThrows()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(new[] { "--window" }));
    }
}
