using playbk.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

namespace playbk.Tests;

public class StepModelTests
{
    private static Script Deserialize(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<Script>(yaml) ?? new Script();
    }

    [Fact]
    public void DeserializesSleepAction()
    {
        var yaml = """
            steps:
              - name: Wait
                action: sleep
                args: "2"
            """;
        var script = Deserialize(yaml);
        Assert.Single(script.Steps);
        Assert.Equal("sleep", script.Steps[0].Action);
        Assert.Equal("2", script.Steps[0].Args);
    }

    [Fact]
    public void DeserializesClickTextAction()
    {
        var yaml = """
            steps:
              - name: Click menu
                action: click-text
                window: "My App*"
                text: "File"
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("click-text", step.Action);
        Assert.Equal("My App*", step.Window);
        Assert.Equal("File", step.Text);
    }

    [Fact]
    public void DeserializesAssertTextAction()
    {
        var yaml = """
            steps:
              - name: Verify banner
                action: assert-text
                window: "My App*"
                text: "Connected"
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("assert-text", step.Action);
        Assert.Equal("My App*", step.Window);
        Assert.Equal("Connected", step.Text);
    }

    [Fact]
    public void DeserializesTypeAction()
    {
        var yaml = """
            steps:
              - name: Fill URL
                action: type
                window: "My App*"
                text: "http://example.com"
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("type", step.Action);
        Assert.Equal("My App*", step.Window);
        Assert.Equal("http://example.com", step.Text);
    }

    [Fact]
    public void DeserializesScreenshotAction()
    {
        var yaml = """
            steps:
              - name: Capture
                action: screenshot
                window: "App*"
                output: "screenshots/main.png"
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("screenshot", step.Action);
        Assert.Equal("App*", step.Window);
        Assert.Equal("screenshots/main.png", step.Output);
    }

    [Fact]
    public void DeserializesWaitWindowAction()
    {
        var yaml = """
            steps:
              - name: Wait for window
                action: wait-window
                window: "My App*"
                timeout: 15
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("wait-window", step.Action);
        Assert.Equal("My App*", step.Window);
        Assert.Equal(15, step.Timeout);
    }

    [Fact]
    public void DeserializesExecWithNewFields()
    {
        var yaml = """
            steps:
              - name: Run something
                action: exec
                args: echo hello
                wait: true
                window: ignored
                text: ignored
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("exec", step.Action);
        Assert.Equal("echo hello", step.Args);
        Assert.True(step.Wait);
        Assert.Equal("ignored", step.Window);
    }

    [Fact]
    public void DeserializesMultipleSteps()
    {
        var yaml = """
            steps:
              - name: Step 1
                action: sleep
                args: "1"
              - name: Step 2
                action: click-text
                window: "App*"
                text: "OK"
              - name: Step 3
                action: screenshot
                window: "App*"
                output: "out.png"
            """;
        var script = Deserialize(yaml);
        Assert.Equal(3, script.Steps.Count);
    }

    [Fact]
    public void DefaultValuesForOptionalFields()
    {
        var yaml = """
            steps:
              - name: Basic
                action: exec
                args: test
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Null(step.Window);
        Assert.Null(step.Text);
        Assert.Null(step.Output);
        Assert.Null(step.Image);
        Assert.Null(step.Voice);
        Assert.Null(step.Timeout);
        Assert.Null(step.Id);
        Assert.False(step.Wait);
        Assert.Equal(0, step.Retries);
        Assert.Null(step.RetryDelay);
        Assert.False(step.ContinueOnError);
        Assert.False(step.Background);
    }

    [Fact]
    public void DeserializesRetryFields()
    {
        var yaml = """
            steps:
              - name: Flaky assert
                action: assert-text
                window: "My App*"
                text: "Connected"
                retries: 4
                retry_delay: 2
                continue_on_error: true
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal(4, step.Retries);
        Assert.Equal(2, step.RetryDelay);
        Assert.True(step.ContinueOnError);
    }

    [Fact]
    public void DeserializesBackgroundTypeAction()
    {
        var yaml = """
            steps:
              - name: Type into a hidden window
                action: type
                window: "Editor*"
                text: "hello"
                background: true
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("type", step.Action);
        Assert.True(step.Background);
    }

    [Fact]
    public void DeserializesHoldAction()
    {
        var yaml = """
            steps:
              - name: Hold a key
                action: hold
                window: "My App*"
                text: "F"
                duration: 30
                method: background
                interval: 50
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("hold", step.Action);
        Assert.Equal("F", step.Text);
        Assert.Equal(30, step.Duration);
        Assert.Equal("background", step.Method);
        Assert.Equal(50, step.Interval);
    }

    [Fact]
    public void DeserializesUiaSetValueAction()
    {
        var yaml = """
            steps:
              - name: Fill the URL field
                action: set-value
                window: "My App*"
                automation_id: "UrlField"
                text: "http://example.com"
              - name: Save
                action: invoke
                window: "My App*"
                element: "Save"
              - name: Toggle telemetry
                action: toggle
                window: "My App*"
                control_type: CheckBox
            """;
        var script = Deserialize(yaml);
        Assert.Equal("set-value", script.Steps[0].Action);
        Assert.Equal("UrlField", script.Steps[0].AutomationId);
        Assert.Equal("http://example.com", script.Steps[0].Text);
        Assert.Equal("Save", script.Steps[1].Element);
        Assert.Equal("CheckBox", script.Steps[2].ControlType);
    }

    [Fact]
    public void DeserializesSpeakAction()
    {
        var yaml = """
            steps:
              - name: Narrate intro
                action: speak
                text: "Welcome to the walkthrough."
                voice: Zira
                output: narration/intro.wav
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("speak", step.Action);
        Assert.Equal("Welcome to the walkthrough.", step.Text);
        Assert.Equal("Zira", step.Voice);
        Assert.Equal("narration/intro.wav", step.Output);
    }

    [Fact]
    public void DeserializesSpeakActionWithDefaults()
    {
        var yaml = """
            steps:
              - name: Quick narration
                action: speak
                text: "Hello."
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("speak", step.Action);
        Assert.Equal("Hello.", step.Text);
        Assert.Null(step.Voice);
        Assert.Null(step.Output);
    }

    [Fact]
    public void DeserializesWaitWindowWithText()
    {
        // Repro of the silent-no-op bug: text: must round-trip onto Step.Text
        // so the runner can shell out to waitfr.exe instead of dropping it.
        var yaml = """
            steps:
              - name: Wait for main menu
                action: wait-window
                window: "MyApp*"
                text: "Start"
                timeout: 45
            """;
        var script = Deserialize(yaml);
        var step = script.Steps[0];
        Assert.Equal("wait-window", step.Action);
        Assert.Equal("MyApp*", step.Window);
        Assert.Equal("Start", step.Text);
        Assert.Equal(45, step.Timeout);
    }
}
