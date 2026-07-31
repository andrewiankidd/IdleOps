using IdleOps.Shared.Windows.Uia;
using Xunit;

namespace IdleOps.Shared.Tests;

public class ControlTypesTests
{
    [Theory]
    [InlineData(50004, "Edit")]
    [InlineData(50000, "Button")]
    [InlineData(50030, "Document")]
    [InlineData(50002, "CheckBox")]
    public void Name_KnownIds_MapToNames(int id, string expected)
    {
        Assert.Equal(expected, ControlTypes.Name(id));
    }

    [Fact]
    public void Name_Zero_IsQuestionMark()
    {
        Assert.Equal("?", ControlTypes.Name(0));
    }

    [Fact]
    public void Name_UnknownId_FallsBackToNumber()
    {
        Assert.Equal("99999", ControlTypes.Name(99999));
    }

    [Theory]
    [InlineData("Edit", 50004)]
    [InlineData("edit", 50004)]
    [InlineData("EDIT", 50004)]
    [InlineData("Document", 50030)]
    public void Parse_Name_IsCaseInsensitive(string token, int expected)
    {
        Assert.Equal(expected, ControlTypes.Parse(token));
    }

    [Fact]
    public void Parse_NumericToken_ReturnsThatId()
    {
        Assert.Equal(50004, ControlTypes.Parse("50004"));
        Assert.Equal(12345, ControlTypes.Parse("12345"));
    }

    [Fact]
    public void Parse_UnknownName_ReturnsNull()
    {
        Assert.Null(ControlTypes.Parse("NotAControlType"));
    }

    [Theory]
    [InlineData(50000)]
    [InlineData(50004)]
    [InlineData(50030)]
    [InlineData(50038)]
    public void Parse_RoundTripsWithName(int id)
    {
        Assert.Equal(id, ControlTypes.Parse(ControlTypes.Name(id)));
    }

    [Theory]
    [InlineData(new[] { "invoke" }, "invoke")]
    [InlineData(new[] { "select", "invoke" }, "invoke")]
    [InlineData(new[] { "select" }, "select")]
    [InlineData(new[] { "toggle" }, "toggle")]
    [InlineData(new[] { "expand-collapse" }, "expand")]
    [InlineData(new[] { "value" }, null)]
    public void ElementInfo_ClickVerb_PrefersInvoke(string[] patterns, string? expected)
    {
        var info = new ElementInfo("Button", null, null, patterns);
        Assert.Equal(expected, info.ClickVerb);
    }

    [Fact]
    public void ElementInfo_HasSelector()
    {
        Assert.True(new ElementInfo("Button", "SaveBtn", null, []).HasSelector);
        Assert.True(new ElementInfo("Button", null, "Save", []).HasSelector);
        Assert.False(new ElementInfo("Button", null, null, []).HasSelector);
    }
}
