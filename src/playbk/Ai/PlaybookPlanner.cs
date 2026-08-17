using llmctl.Llm;
using playbk.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace playbk.Ai;

/// <summary>
/// Turns a natural-language goal into a runnable playbook by handing the model the
/// playbk action vocabulary as its tool schema (every DispatchStepAsync action is a
/// tool; a Step is a tool call) and validating what comes back. This is the
/// "generator" slice: plan the whole playbook up front, let the user review it,
/// then run it deterministically.
/// </summary>
internal static class PlaybookPlanner
{
    /// <summary>
    /// The actions the runner knows (must track ScriptRunner.DispatchStepAsync).
    /// Used both to teach the model and to reject a plan that invents an action.
    /// </summary>
    public static readonly IReadOnlySet<string> KnownActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "exec", "sleep", "wait-window", "screenshot", "click-text", "assert-text",
        "type", "keyboard", "keys", "chord", "speak", "set-value", "invoke", "toggle",
        "expand", "collapse", "select", "assert-value", "hold",
    };

    /// <summary>
    /// The tool schema, as a system prompt. Describes each action and its fields so
    /// the model emits a valid playbk Script and nothing else.
    /// </summary>
    public const string SystemPrompt = """
        You are IdleOps' playbook planner. Convert the user's goal into a YAML playbook that the
        IdleOps `playbk` engine runs on Windows. Output ONLY YAML — no prose, no code fences.

        The root is `steps:`, a list. Every step has `name:` (a short human label) and `action:`.
        Actions and their fields (use only these actions):

          exec         args: <command or URL to launch, e.g. notepad.exe>. Optional id: <token> lets
                       later steps reference %id_pid%. wait: true blocks until the process exits.
          sleep        timeout: <seconds>.
          wait-window  window: <title substring>. Optional text: <OCR text to wait for>. timeout: <seconds>.
          screenshot   window: <title substring>. output: <png path>.
          type         window: <title substring>. text: <text to type into the focused control>.
          keyboard     window: <title substring>. text: <key chord/sequence, e.g. "CTRL+S", "ALT+F4",
                       "CTRL+A, DELETE">. Modifiers CTRL/ALT/SHIFT/WIN; keys F1-F12, ENTER, TAB, ESC,
                       arrows, etc. Use this for keyboard shortcuts (saving, closing, select-all).
          click-text   window: <title substring>. text: <on-screen text to OCR-locate and click>.
          assert-text  window: <title substring>. text: <on-screen text that must be present>.
          set-value    window + one selector + text: <value>.
          invoke       window + selector. Presses/activates the element (buttons, menu items).
          toggle/expand/collapse/select   window + selector.
          assert-value window + selector + text: <expected value>.
          hold         window + text: <key(s) to hold> + duration: <seconds>.

        A "selector" is exactly ONE of these fields (not a field literally named `selector`):
          automation_id: <UIA AutomationId>   element: <accessibility Name>   control_type: <e.g. Button>
        To save/close/etc, prefer a `keyboard` shortcut (e.g. CTRL+S) over hunting for menu items.
        For a Save-As dialog: keyboard CTRL+S, then type the filename, then keyboard ENTER.

        Any step may add: wait, retries, retry_delay, continue_on_error.
        Prefer launching apps with `exec`, waiting for their window with `wait-window`, then acting.
        Keep it minimal and robust. If a goal cannot be done with these actions, emit a single
        `speak` step whose text explains what is missing.

        Example goal: "open notepad and type hello"
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
            text: hello
        """;

    /// <summary>Generate a playbook for <paramref name="goal"/>. Returns the raw model output and which backend served it.</summary>
    public static async Task<(string Raw, string? Backend, IReadOnlyList<string> Errors)> GenerateAsync(string goal, CancellationToken token)
    {
        var result = await LlmRunner.CompleteAsync(
            system: SystemPrompt,
            goal: goal,
            imageBase64: null,
            temperature: 0.2,   // low: we want structured YAML, not creativity
            token: token);

        return (result.Text, result.Backend, result.Errors);
    }

    /// <summary>
    /// Strip any markdown fences / leading prose a model may add, keeping from the
    /// first `steps:` line onward — so a chatty model still yields parseable YAML.
    /// </summary>
    public static string ExtractYaml(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var text = raw.Replace("\r\n", "\n").Trim();

        // If a ```...``` fence appears anywhere (even after chatty preamble), keep
        // only its contents.
        var fenceOpen = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceOpen >= 0)
        {
            var afterOpenLine = text.IndexOf('\n', fenceOpen);   // skip the ```/```yaml line
            if (afterOpenLine >= 0)
            {
                var rest = text[(afterOpenLine + 1)..];
                var fenceClose = rest.IndexOf("```", StringComparison.Ordinal);
                text = (fenceClose >= 0 ? rest[..fenceClose] : rest).Trim();
            }
        }

        // Trim any preamble before the first top-level `steps:`.
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("steps:", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join('\n', lines[i..]).Trim();
            }
        }

        return text;
    }

    /// <summary>
    /// Parse model output into a Script and validate it: must have at least one step,
    /// every step must name a known action. Returns the Script (null on failure) and
    /// a list of human-readable problems.
    /// </summary>
    public static (Script? Script, IReadOnlyList<string> Errors) ParseAndValidate(string raw)
    {
        var errors = new List<string>();
        var yaml = ExtractYaml(raw);
        if (string.IsNullOrWhiteSpace(yaml))
        {
            errors.Add("model returned no YAML.");
            return (null, errors);
        }

        Script? script;
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            script = deserializer.Deserialize<Script>(yaml);
        }
        catch (Exception ex)
        {
            errors.Add($"YAML did not parse: {ex.Message}");
            return (null, errors);
        }

        if (script is null || script.Steps.Count == 0)
        {
            errors.Add("playbook has no steps.");
            return (null, errors);
        }

        for (var i = 0; i < script.Steps.Count; i++)
        {
            var step = script.Steps[i];
            if (string.IsNullOrWhiteSpace(step.Action))
                errors.Add($"step {i + 1} ('{step.Name}') has no action.");
            else if (!KnownActions.Contains(step.Action))
                errors.Add($"step {i + 1} ('{step.Name}') uses unknown action '{step.Action}'.");
        }

        return errors.Count == 0 ? (script, errors) : (null, errors);
    }
}
