namespace playbk.Model;

internal sealed class Script
{
    public List<Step> Steps { get; set; } = new();

    // Optional media capture for the whole run (snake_case in YAML: vidcap / audcap).
    // When set, ScriptRunner records for captureTimerSeconds while the steps execute.
    public bool Vidcap { get; set; }
    public bool Audcap { get; set; }
}

internal sealed class Step
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Args { get; set; }
    public bool Wait { get; set; }

    // Extended fields for built-in actions
    public string? Window { get; set; }
    public string? Text { get; set; }
    public string? Output { get; set; }
    public string? Image { get; set; }
    public string? Voice { get; set; }
    public double? Timeout { get; set; }

    // Retry / error handling (Ansible / AzureDevOps style)
    public int Retries { get; set; }            // extra attempts after the first failure (default 0 = fail fast)
    public double? RetryDelay { get; set; }     // seconds to wait between attempts (default 0)
    public bool ContinueOnError { get; set; }   // if true, a failed step is logged and the flow continues

    // type: drive a background window via PostMessage instead of foregrounding it
    // (no focus steal). Classic Win32 controls only, not webviews. Default false.
    public bool Background { get; set; }

    // UI Automation selectors (set-value / assert-value / invoke / toggle /
    // expand / collapse / select actions, driven via uiactl). Pick one selector;
    // automation_id wins, then element (accessibility Name), then control_type.
    public string? AutomationId { get; set; }
    public string? Element { get; set; }
    public string? ControlType { get; set; }

    // hold: sustained key input (via inpctl). text = key(s) to hold.
    public double? Duration { get; set; }       // seconds to hold (required for the hold action)
    public string? Method { get; set; }         // delivery: foreground | background
    public int? Interval { get; set; }          // ms between re-posts (background method)
}
