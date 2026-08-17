using System;
using inpctl.Cli;
using Xunit;

namespace inpctl.Tests;

public class OptionsParserTests
{
    [Fact]
    public void Parse_HoldWithDurationMethodInterval()
    {
        var o = OptionsParser.Parse(["-w", "App*", "--hold", "F", "--duration", "60", "--method", "background", "--interval", "50"]);
        Assert.Equal("F", o.Hold);
        Assert.Equal(60, o.Duration);
        Assert.Equal(InputMethod.Background, o.Method);
        Assert.Equal(50, o.Interval);
        Assert.True(o.HasAction);
    }

    [Fact]
    public void Parse_HoldDefaults()
    {
        var o = OptionsParser.Parse(["-w", "App*", "--hold", "W"]);
        Assert.Equal(InputMethod.Foreground, o.Method);  // default
        Assert.Equal(30, o.Interval);                    // default
        Assert.Equal(0, o.Duration);                     // default = indefinite
    }

    [Theory]
    [InlineData("foreground", "Foreground")]
    [InlineData("fg", "Foreground")]
    [InlineData("background", "Background")]
    [InlineData("BG", "Background")]
    public void ParseMethod_AcceptsNamesAndAliases(string token, string expected)
    {
        Assert.Equal(expected, OptionsParser.ParseMethod(token).ToString());
    }

    [Fact]
    public void ParseMethod_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.ParseMethod("sideways"));
    }

    [Fact]
    public void Parse_InvalidDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--hold", "F", "--duration", "soon"]));
    }

    [Fact]
    public void Parse_InvalidInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["--hold", "F", "--interval", "fast"]));
    }
}
