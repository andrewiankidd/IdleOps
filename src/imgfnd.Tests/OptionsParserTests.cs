using imgfnd.Cli;
using Xunit;

namespace imgfnd.Tests;

public class OptionsParserTests
{
    [Fact]
    public void Parse_WindowImageThreshold()
    {
        var o = OptionsParser.Parse(["-w", "App*", "-i", "ref.png", "--threshold", "0.9"]);
        Assert.Equal("App*", o.Window);
        Assert.Equal("ref.png", o.ImagePath);
        Assert.Equal(0.9, o.Threshold);
    }

    [Fact]
    public void Parse_DefaultThreshold()
    {
        var o = OptionsParser.Parse(["-w", "App*", "-i", "ref.png"]);
        Assert.Equal(0.8, o.Threshold);
    }

    [Fact]
    public void Parse_HelpAndVersionFlags()
    {
        Assert.True(OptionsParser.Parse(["-h"]).ShowHelp);
        Assert.True(OptionsParser.Parse(["--version"]).ShowVersion);
    }

    [Fact]
    public void Parse_InvalidThreshold_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--threshold", "high"]));
    }

    [Fact]
    public void Parse_UnknownArg_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--nope"]));
    }
}
