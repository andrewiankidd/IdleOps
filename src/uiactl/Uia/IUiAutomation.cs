using IdleOps.Shared.Windows.Uia;

namespace uiactl.Uia;

/// <summary>
/// Element-level automation over a platform's accessibility tree. Windows uses UI
/// Automation (COM); Linux uses AT-SPI2 (via a pyatspi helper). Selected by
/// <see cref="UiAutomationFactory"/>. Returns the shared UIA result types so the CLI
/// stays platform-agnostic.
/// </summary>
internal interface IUiAutomation
{
    string Name { get; }
    ElementInfo? ElementAt(int x, int y);
    DumpResult? Dump(string window, int max);
    UiaResult SetValue(string window, Selector selector, string value);
    UiaResult GetValue(string window, Selector selector);
    UiaResult Invoke(string window, Selector selector);
    UiaResult Toggle(string window, Selector selector);
    UiaResult ExpandCollapse(string window, Selector selector, bool expand);
    UiaResult Select(string window, Selector selector);
}
