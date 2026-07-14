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
}
