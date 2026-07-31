using System.Runtime.Versioning;

namespace IdleOps.Shared.Windows.Uia;

/// <summary>Element selector: AutomationId wins, then Name, then ControlType.</summary>
public readonly record struct Selector(string? AutomationId, string? Name, int? ControlType)
{
    public bool IsEmpty => string.IsNullOrEmpty(AutomationId) && string.IsNullOrEmpty(Name) && ControlType is null;
}

/// <summary>Outcome of a UIA verb. Value is populated by get-value.</summary>
public sealed record UiaResult(bool Ok, string Message, string? Value = null)
{
    public static UiaResult Fail(string message) => new(false, message);
    public static UiaResult Done(string message) => new(true, message);
    public static UiaResult WithValue(string value) => new(true, string.Empty, value);
}

/// <summary>Result of a dump: total element count plus the (capped) shown list.</summary>
public sealed record DumpResult(int Total, IReadOnlyList<ElementInfo> Shown);

/// <summary>
/// Element-level desktop automation via UI Automation. The single home for UIA
/// logic — uiactl (CLI) and stpcap (recorder) both build on this. Resolves a
/// window + selector and drives controls through UIA patterns; also answers
/// point queries (element under a screen coordinate). Windows-only.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UiaAutomation
{
    private readonly IUIAutomation _uia;

    public UiaAutomation()
    {
        IdleOps.Shared.Platform.PlatformSupport.RequireWindows("UI Automation");
        _uia = (IUIAutomation)new CUIAutomation();
    }

    /// <summary>Describe the element at a screen point (best-effort, null on failure).</summary>
    public ElementInfo? ElementAt(int x, int y)
    {
        try
        {
            var el = _uia.ElementFromPoint(new UiaPoint { X = x, Y = y });
            return el is null ? null : Describe(el);
        }
        catch
        {
            return null;
        }
    }

    public UiaResult SetValue(string window, Selector selector, string value)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var vp = Pattern<IUIAutomationValuePattern>(el, UiaIds.ValuePatternId);
        if (vp is null) return UiaResult.Fail("element does not support ValuePattern.");
        if (vp.CurrentIsReadOnly() != 0) return UiaResult.Fail("element value is read-only.");
        vp.SetValue(value);
        return UiaResult.Done($"set value ({value.Length} chars).");
    }

    public UiaResult GetValue(string window, Selector selector)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var vp = Pattern<IUIAutomationValuePattern>(el, UiaIds.ValuePatternId);
        if (vp is null) return UiaResult.Fail("element does not support ValuePattern.");
        return UiaResult.WithValue(vp.CurrentValue());
    }

    public UiaResult Invoke(string window, Selector selector)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var ip = Pattern<IUIAutomationInvokePattern>(el, UiaIds.InvokePatternId);
        if (ip is null) return UiaResult.Fail("element does not support InvokePattern.");
        ip.Invoke();
        return UiaResult.Done("invoked.");
    }

    public UiaResult Toggle(string window, Selector selector)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var tp = Pattern<IUIAutomationTogglePattern>(el, UiaIds.TogglePatternId);
        if (tp is null) return UiaResult.Fail("element does not support TogglePattern.");
        tp.Toggle();
        return UiaResult.Done($"toggled (state now {tp.CurrentToggleState()}).");
    }

    public UiaResult ExpandCollapse(string window, Selector selector, bool expand)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var ep = Pattern<IUIAutomationExpandCollapsePattern>(el, UiaIds.ExpandCollapsePatternId);
        if (ep is null) return UiaResult.Fail("element does not support ExpandCollapsePattern.");
        if (expand) ep.Expand(); else ep.Collapse();
        return UiaResult.Done(expand ? "expanded." : "collapsed.");
    }

    public UiaResult Select(string window, Selector selector)
    {
        var el = Resolve(window, selector, out var error);
        if (el is null) return UiaResult.Fail(error);
        var sp = Pattern<IUIAutomationSelectionItemPattern>(el, UiaIds.SelectionItemPatternId);
        if (sp is null) return UiaResult.Fail("element does not support SelectionItemPattern.");
        sp.Select();
        return UiaResult.Done("selected.");
    }

    public DumpResult? Dump(string window, int max)
    {
        var root = RootFor(window);
        if (root is null) return null;
        var all = root.FindAll(TreeScope.Descendants, _uia.CreateTrueCondition());
        var total = all.Length();
        var shown = Math.Min(total, max);
        var items = new List<ElementInfo>(shown);
        for (var i = 0; i < shown; i++)
        {
            items.Add(Describe(all.GetElement(i)));
        }
        return new DumpResult(total, items);
    }

    // --- internals ---

    private IUIAutomationElement? RootFor(string window)
    {
        var handle = WindowMatcher.FindWindow(window, preferNewest: true)?.Handle ?? IntPtr.Zero;
        return handle == IntPtr.Zero ? null : _uia.ElementFromHandle(handle);
    }

    private IUIAutomationElement? Resolve(string window, Selector selector, out string error)
    {
        error = string.Empty;
        var root = RootFor(window);
        if (root is null) { error = $"window '{window}' not found."; return null; }

        IUIAutomationCondition condition;
        if (!string.IsNullOrEmpty(selector.AutomationId))
            condition = _uia.CreatePropertyCondition(UiaIds.AutomationIdPropertyId, selector.AutomationId);
        else if (!string.IsNullOrEmpty(selector.Name))
            condition = _uia.CreatePropertyCondition(UiaIds.NamePropertyId, selector.Name);
        else if (selector.ControlType is int ct)
            condition = _uia.CreatePropertyCondition(UiaIds.ControlTypePropertyId, ct);
        else { error = "no selector (automation-id / name / control-type)."; return null; }

        var found = root.FindFirst(TreeScope.Descendants, condition);
        if (found is null) error = "element not found for the given selector.";
        return found;
    }

    private static T? Pattern<T>(IUIAutomationElement el, int patternId) where T : class
        => el.GetCurrentPattern(patternId) as T;

    private static ElementInfo Describe(IUIAutomationElement el)
    {
        var controlType = ControlTypes.Name(SafeControlType(el));
        var autoId = Safe(() => el.CurrentAutomationId());
        var name = Safe(() => el.CurrentName());
        return new ElementInfo(
            controlType,
            string.IsNullOrEmpty(autoId) ? null : autoId,
            string.IsNullOrEmpty(name) ? null : name,
            SupportedPatterns(el));
    }

    private static IReadOnlyList<string> SupportedPatterns(IUIAutomationElement el)
    {
        var names = new List<string>();
        if (Pattern<IUIAutomationValuePattern>(el, UiaIds.ValuePatternId) is not null) names.Add("value");
        if (Pattern<IUIAutomationInvokePattern>(el, UiaIds.InvokePatternId) is not null) names.Add("invoke");
        if (Pattern<IUIAutomationTogglePattern>(el, UiaIds.TogglePatternId) is not null) names.Add("toggle");
        if (Pattern<IUIAutomationExpandCollapsePattern>(el, UiaIds.ExpandCollapsePatternId) is not null) names.Add("expand-collapse");
        if (Pattern<IUIAutomationSelectionItemPattern>(el, UiaIds.SelectionItemPatternId) is not null) names.Add("select");
        return names;
    }

    private static int SafeControlType(IUIAutomationElement el)
    {
        try { return el.CurrentControlType(); }
        catch { return 0; }
    }

    private static string Safe(Func<string> get)
    {
        try { return get() ?? string.Empty; }
        catch { return string.Empty; }
    }
}
