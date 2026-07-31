using waitfr.Cli;
using Xunit;

namespace waitfr.Tests;

public class OptionsParserTests
{
    [Fact]
    public void Parse_WindowTextTimeout()
    {
        var o = OptionsParser.Parse(["-w", "My App*", "-t", "Ready", "--timeout", "30"]);
        Assert.Equal("My App*", o.Window);
        Assert.Equal("Ready", o.Text);
        Assert.Equal(30, o.Timeout);
        Assert.False(o.Gone);
    }

    [Fact]
    public void Parse_DefaultTimeoutIsTen()
    {
        var o = OptionsParser.Parse(["-w", "App*"]);
        Assert.Equal(10, o.Timeout);
    }

    [Fact]
    public void Parse_GoneAndHelpFlags()
    {
        var o = OptionsParser.Parse(["--window", "App*", "--gone", "-h"]);
        Assert.True(o.Gone);
        Assert.True(o.ShowHelp);
    }

    [Fact]
    public void Parse_InvalidTimeout_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["-w", "App*", "--timeout", "soon"]));
    }

    [Fact]
    public void Parse_UnknownArg_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--bogus"]));
    }

    [Fact]
    public void Parse_MissingValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["-w"]));
    }
}
