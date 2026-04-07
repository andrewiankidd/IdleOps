using txtfnd.Cli;
using Xunit;

namespace txtfnd.Tests;

public class OptionsParserTests
{
    [Fact]
    public void ParsesWindowAndText()
    {
        var opts = OptionsParser.Parse(new[] { "-w", "Notepad*", "-t", "File" });
        Assert.Equal("Notepad*", opts.Window);
        Assert.Equal("File", opts.Text);
        Assert.False(opts.ShowHelp);
    }

    [Fact]
    public void ParsesLongArgs()
    {
        var opts = OptionsParser.Parse(new[] { "--window", "My App", "--text", "OK" });
        Assert.Equal("My App", opts.Window);
        Assert.Equal("OK", opts.Text);
    }

    [Fact]
    public void ParsesHelp()
    {
        var opts = OptionsParser.Parse(new[] { "--help" });
        Assert.True(opts.ShowHelp);
    }

    [Fact]
    public void ParsesVersion()
    {
        var opts = OptionsParser.Parse(new[] { "--version" });
        Assert.True(opts.ShowVersion);
    }

    [Fact]
    public void EmptyArgsReturnsDefaults()
    {
        var opts = OptionsParser.Parse(Array.Empty<string>());
        Assert.Null(opts.Window);
        Assert.Null(opts.Text);
        Assert.False(opts.ShowHelp);
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

    [Fact]
    public void MissingTextValueThrows()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(new[] { "--text" }));
    }
}
