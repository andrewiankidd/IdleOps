using IdleOps.Shared.Windows.Uia;
using stpcap.Recording;
using Xunit;

namespace stpcap.Tests;

public class ScriptGeneratorTests
{
    private static InputEvent LeftClick(string window, ElementInfo? element) =>
        new(InputEventType.MouseClick, DateTime.UtcNow, window, X: 100, Y: 200, Button: "left", Element: element);

    [Fact]
    public void SemanticClick_InvokeByAutomationId()
    {
        var el = new ElementInfo("Button", "SaveButton", "Save", ["invoke"]);
        var yaml = ScriptGenerator.Generate([LeftClick("My App*", el)]);
        Assert.Contains("action: invoke", yaml);
        Assert.Contains("automation_id: \"SaveButton\"", yaml);
        Assert.DoesNotContain("--leftmouse", yaml);
    }

    [Fact]
    public void SemanticClick_InvokeByName_WhenNoAutomationId()
    {
        var el = new ElementInfo("Button", null, "Don't save", ["invoke"]);
        var yaml = ScriptGenerator.Generate([LeftClick("App*", el)]);
        Assert.Contains("action: invoke", yaml);
        Assert.Contains("element: \"Don't save\"", yaml);
    }

    [Fact]
    public void SemanticClick_ToggleForCheckbox()
    {
        var el = new ElementInfo("CheckBox", "TelemetryToggle", "Enable telemetry", ["toggle"]);
        var yaml = ScriptGenerator.Generate([LeftClick("App*", el)]);
        Assert.Contains("action: toggle", yaml);
        Assert.Contains("automation_id: \"TelemetryToggle\"", yaml);
    }

    [Fact]
    public void SemanticClick_FallsBackToClickText_WhenNoVerbButHasName()
    {
        var el = new ElementInfo("Text", null, "Welcome", []);
        var yaml = ScriptGenerator.Generate([LeftClick("App*", el)]);
        Assert.Contains("action: click-text", yaml);
        Assert.Contains("text: \"Welcome\"", yaml);
        Assert.DoesNotContain("--leftmouse", yaml);
    }

    [Fact]
    public void SemanticClick_FallsBackToCoordinates_WhenNoVerbNoName()
    {
        var el = new ElementInfo("Pane", null, null, []);
        var yaml = ScriptGenerator.Generate([LeftClick("App*", el)]);
        Assert.Contains("--leftmouse", yaml);
        Assert.DoesNotContain("action: invoke", yaml);
    }

    [Fact]
    public void RightClickWithElement_StaysCoordinates()
    {
        var el = new ElementInfo("Button", "X", "X", ["invoke"]);
        var events = new List<InputEvent>
        {
            new(InputEventType.MouseClick, DateTime.UtcNow, "App*", X: 5, Y: 5, Button: "right", Element: el)
        };
        var yaml = ScriptGenerator.Generate(events);
        Assert.Contains("--rightmouse", yaml);
        Assert.DoesNotContain("action: invoke", yaml);
    }

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
