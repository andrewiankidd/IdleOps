using playbk.Ai;
using Xunit;

namespace playbk.Tests;

/// <summary>
/// Offline tests for the planner's parse/validate layer — the deterministic half of
/// the AI generator. No model is called; we feed it representative model output.
/// </summary>
public class PlaybookPlannerTests
{
    private const string GoodYaml = """
        steps:
          - id: notepad
            name: Launch Notepad
            action: exec
            args: notepad.exe
          - name: Wait for the Notepad window
            action: wait-window
            window: Notepad
            timeout: 10
          - name: Type the greeting
            action: type
            window: Notepad
            text: hello world
        """;

    [Fact]
    public void ParseAndValidate_AcceptsAValidPlan()
    {
        var (script, errors) = PlaybookPlanner.ParseAndValidate(GoodYaml);
        Assert.NotNull(script);
        Assert.Empty(errors);
        Assert.Equal(3, script!.Steps.Count);
        Assert.Equal("exec", script.Steps[0].Action);
        Assert.Equal("notepad.exe", script.Steps[0].Args);
    }

    [Fact]
    public void ExtractYaml_StripsCodeFences()
    {
        var fenced = "```yaml\n" + GoodYaml + "\n```";
        var yaml = PlaybookPlanner.ExtractYaml(fenced);
        Assert.StartsWith("steps:", yaml);
        Assert.DoesNotContain("```", yaml);
    }

    [Fact]
    public void ExtractYaml_DropsChattyPreamble()
    {
        var chatty = "Sure! Here is your playbook:\n\n" + GoodYaml;
        var yaml = PlaybookPlanner.ExtractYaml(chatty);
        Assert.StartsWith("steps:", yaml);
        Assert.DoesNotContain("Sure!", yaml);
    }

    [Fact]
    public void ParseAndValidate_ToleratesFencedChattyOutput()
    {
        var messy = "Here you go:\n\n```yaml\n" + GoodYaml + "\n```\nHope that helps!";
        var (script, errors) = PlaybookPlanner.ParseAndValidate(messy);
        Assert.NotNull(script);
        Assert.Empty(errors);
    }

    [Fact]
    public void ParseAndValidate_RejectsUnknownAction()
    {
        var bad = """
            steps:
              - name: Do a barrel roll
                action: barrel-roll
            """;
        var (script, errors) = PlaybookPlanner.ParseAndValidate(bad);
        Assert.Null(script);
        Assert.Contains(errors, e => e.Contains("barrel-roll"));
    }

    [Fact]
    public void ParseAndValidate_RejectsEmptyPlan()
    {
        var (script, errors) = PlaybookPlanner.ParseAndValidate("steps: []");
        Assert.Null(script);
        Assert.Contains(errors, e => e.Contains("no steps"));
    }

    [Fact]
    public void ParseAndValidate_RejectsNonYaml()
    {
        var (script, errors) = PlaybookPlanner.ParseAndValidate("I cannot help with that.");
        Assert.Null(script);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void KnownActions_MatchesTheDispatcherVocabulary()
    {
        // Guards against the schema drifting from ScriptRunner.DispatchStepAsync.
        foreach (var action in new[] { "exec", "sleep", "wait-window", "screenshot",
                     "click-text", "assert-text", "type", "keyboard", "speak", "set-value",
                     "invoke", "toggle", "expand", "collapse", "select", "assert-value", "hold" })
        {
            Assert.Contains(action, PlaybookPlanner.KnownActions);
        }
    }
}
