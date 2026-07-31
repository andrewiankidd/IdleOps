namespace IdleOps.Shared.Windows.Uia;

/// <summary>
/// A snapshot of a UIA element: its control type, selectors, and which control
/// patterns it supports. Returned by point queries (stpcap) and dumps (uiactl).
/// Pure data — no COM handles — so it is safe to store and unit-test.
/// </summary>
public sealed record ElementInfo(
    string ControlType,
    string? AutomationId,
    string? Name,
    IReadOnlyList<string> Patterns)
{
    /// <summary>
    /// The best semantic verb for a *click* on this element (invoke &gt; select &gt;
    /// toggle &gt; expand), or null if none applies. Used by stpcap to record a
    /// resilient step instead of raw coordinates.
    /// </summary>
    public string? ClickVerb =>
        Patterns.Contains("invoke") ? "invoke"
        : Patterns.Contains("select") ? "select"
        : Patterns.Contains("toggle") ? "toggle"
        : Patterns.Contains("expand-collapse") ? "expand"
        : null;

    /// <summary>True when a stable selector (AutomationId or Name) is available.</summary>
    public bool HasSelector => !string.IsNullOrEmpty(AutomationId) || !string.IsNullOrEmpty(Name);
}
