namespace uiactl.Cli;

internal record Options
{
    // Target
    public string? Window { get; init; }
    public string? AutomationId { get; init; }
    public string? Name { get; init; }
    public string? ControlType { get; init; }

    // Verbs
    public string? SetValue { get; init; }
    public bool GetValue { get; init; }
    public bool Invoke { get; init; }
    public bool Toggle { get; init; }
    public bool Expand { get; init; }
    public bool Collapse { get; init; }
    public bool Select { get; init; }
    public bool Dump { get; init; }
    public int Max { get; init; } = 60;
    public string? ElementAt { get; init; }

    public bool ShowHelp { get; init; }

    public bool HasVerb =>
        SetValue is not null || GetValue || Invoke || Toggle || Expand || Collapse || Select || Dump
        || ElementAt is not null;

    public bool HasSelector =>
        !string.IsNullOrEmpty(AutomationId) || !string.IsNullOrEmpty(Name) || !string.IsNullOrEmpty(ControlType);
}
