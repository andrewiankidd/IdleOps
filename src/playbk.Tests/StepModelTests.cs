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
        Assert.Null(step.Timeout);
        Assert.Null(step.Id);
        Assert.False(step.Wait);
    }
}
