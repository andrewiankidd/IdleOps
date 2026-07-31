using uiactl.Cli;
using Xunit;

namespace uiactl.Tests;

public class OptionsParserTests
{
    [Fact]
    public void Parse_WindowAndAutomationIdAndSetValue()
    {
        var o = OptionsParser.Parse(["-w", "My App*", "--automation-id", "UrlField", "--set-value", "hello"]);
        Assert.Equal("My App*", o.Window);
        Assert.Equal("UrlField", o.AutomationId);
        Assert.Equal("hello", o.SetValue);
        Assert.True(o.HasVerb);
        Assert.True(o.HasSelector);
    }

    [Fact]
    public void Parse_NameSelectorAndInvokeFlag()
    {
        var o = OptionsParser.Parse(["--window", "App*", "--name", "Don't save", "--invoke"]);
        Assert.Equal("Don't save", o.Name);
        Assert.True(o.Invoke);
        Assert.True(o.HasSelector);
    }

    [Fact]
    public void Parse_ControlTypeSelector()
    {
        var o = OptionsParser.Parse(["-w", "App*", "--control-type", "CheckBox", "--toggle"]);
        Assert.Equal("CheckBox", o.ControlType);
        Assert.True(o.Toggle);
    }

    [Fact]
    public void Parse_DumpWithMax()
    {
        var o = OptionsParser.Parse(["-w", "App*", "--dump", "--max", "25"]);
        Assert.True(o.Dump);
        Assert.Equal(25, o.Max);
        Assert.True(o.HasVerb);
        Assert.False(o.HasSelector);
    }

    [Fact]
    public void Parse_NoVerb_NoSelector()
    {
        var o = OptionsParser.Parse(["-w", "App*"]);
        Assert.False(o.HasVerb);
        Assert.False(o.HasSelector);
    }

    [Fact]
    public void Parse_HelpFlag()
    {
        var o = OptionsParser.Parse(["-h"]);
        Assert.True(o.ShowHelp);
    }

    [Fact]
    public void Parse_InvalidMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(["-w", "App*", "--dump", "--max", "notanumber"]));
    }

    [Fact]
    public void Parse_DefaultMaxIsSixty()
    {
        var o = OptionsParser.Parse(["-w", "App*", "--dump"]);
        Assert.Equal(60, o.Max);
    }
}
