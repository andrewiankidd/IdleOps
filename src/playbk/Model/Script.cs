namespace playbk.Model;

internal sealed class Script
{
    public List<Step> Steps { get; set; } = new();
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
    public double? Timeout { get; set; }
}
