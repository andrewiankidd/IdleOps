using System.Runtime.Versioning;
using IdleOps.Shared.Windows.Uia;

namespace uiactl.Uia;

/// <summary>Windows backend: delegates to the shared UI Automation (COM) engine.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsUiAutomation : IUiAutomation
{
    private readonly UiaAutomation _uia = new();

    public string Name => "uia";

    public ElementInfo? ElementAt(int x, int y) => _uia.ElementAt(x, y);
    public DumpResult? Dump(string window, int max) => _uia.Dump(window, max);
    public UiaResult SetValue(string window, Selector selector, string value) => _uia.SetValue(window, selector, value);
    public UiaResult GetValue(string window, Selector selector) => _uia.GetValue(window, selector);
    public UiaResult Invoke(string window, Selector selector) => _uia.Invoke(window, selector);
    public UiaResult Toggle(string window, Selector selector) => _uia.Toggle(window, selector);
    public UiaResult ExpandCollapse(string window, Selector selector, bool expand) => _uia.ExpandCollapse(window, selector, expand);
    public UiaResult Select(string window, Selector selector) => _uia.Select(window, selector);
}
