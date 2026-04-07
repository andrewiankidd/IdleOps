using stpcap.Recording;
using Xunit;

namespace stpcap.Tests;

public class ScriptGeneratorTests
{
    [Fact]
    public void EmptyEventsProducesMinimalYaml()
    {
        var yaml = ScriptGenerator.Generate([]);
        Assert.Contains("steps:", yaml);
        Assert.DoesNotContain("action:", yaml);
    }

    [Fact]
    public void MouseClickGeneratesInpctlStep()
    {
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseClick, DateTime.UtcNow, "Test Window", X: 100, Y: 200, Button: "left")
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("--leftmouse", yaml);
        Assert.Contains("action: exec", yaml);
    }

    [Fact]
    public void RightClickGeneratesRightMouseFlag()
    {
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseClick, DateTime.UtcNow, "Test", X: 50, Y: 50, Button: "right")
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("--rightmouse", yaml);
    }

    [Fact]
    public void MouseDragGeneratesDragCoords()
    {
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseDrag, DateTime.UtcNow, "Test", X: 10, Y: 20, EndX: 100, EndY: 200, Button: "left")
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("--move-cursor", yaml);
        Assert.Contains("-", yaml); // drag separator
    }

    [Fact]
    public void TextInputGeneratesTypeStep()
    {
        var events = new List<InputEvent>
        {
            new(InputEventType.TextInput, DateTime.UtcNow, "Notepad") { Button = "hello world" }
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("--type", yaml);
        Assert.Contains("hello world", yaml);
    }

    [Fact]
    public void LargeGapInsertsSleep()
    {
        var t1 = DateTime.UtcNow;
        var t2 = t1.AddSeconds(3);
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseClick, t1, "Test", X: 10, Y: 10, Button: "left"),
            new(InputEventType.MouseClick, t2, "Test", X: 20, Y: 20, Button: "left")
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("action: sleep", yaml);
    }

    [Fact]
    public void SmallGapDoesNotInsertSleep()
    {
        var t1 = DateTime.UtcNow;
        var t2 = t1.AddMilliseconds(100);
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseClick, t1, "Test", X: 10, Y: 10, Button: "left"),
            new(InputEventType.MouseClick, t2, "Test", X: 20, Y: 20, Button: "left")
        };

        var yaml = ScriptGenerator.Generate(events);
        Assert.DoesNotContain("action: sleep", yaml);
    }

    [Fact]
    public void YamlOutputStartsWithHeader()
    {
        var yaml = ScriptGenerator.Generate([]);
        Assert.StartsWith("# Recorded by stpcap", yaml);
    }
}
