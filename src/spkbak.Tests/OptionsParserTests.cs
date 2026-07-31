using spkbak.Cli;
using Xunit;

namespace spkbak.Tests;

public class OptionsParserTests
{
    [Fact]
    public void Parse_TextVoiceOutput()
    {
        var o = OptionsParser.Parse(["--text", "hello", "--voice", "Zira", "-o", "out.wav"]);
        Assert.Equal("hello", o.Text);
        Assert.Equal("Zira", o.Voice);
        Assert.Equal("out.wav", o.Output);
    }

    [Fact]
    public void Parse_FileAndListFlags()
    {
        var o = OptionsParser.Parse(["-f", "script.txt", "--list"]);
        Assert.Equal("script.txt", o.File);
        Assert.True(o.List);
    }

    [Fact]
    public void Parse_HelpFlag()
    {
        Assert.True(OptionsParser.Parse(["-h"]).ShowHelp);
    }

    [Fact]
    public void Parse_UnknownArg_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--nope"]));
    }

    [Fact]
    public void Parse_MissingValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--text"]));
    }
}
