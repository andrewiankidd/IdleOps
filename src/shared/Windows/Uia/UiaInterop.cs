using System.Runtime.InteropServices;

namespace IdleOps.Shared.Windows.Uia;

// Raw COM interop for UI Automation (UIAutomationCore.dll). No NuGet — we declare
// only the vtable slots we call, with `void _slotN()` stubs filling the gaps so
// method ordering matches the native vtable exactly. Unused stubs are never
// invoked, so their (empty) signatures are irrelevant; only their position counts.
// This is the single home for UIA interop (uiactl and stpcap both build on it).

[ComImport]
[Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
internal class CUIAutomation
{
}

[StructLayout(LayoutKind.Sequential)]
internal struct UiaPoint
{
    public int X;
    public int Y;
}

[ComImport]
[Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomation
{
    void _CompareElements();                                    // 1
    void _CompareRuntimeIds();                                  // 2
    IUIAutomationElement GetRootElement();                      // 3
    IUIAutomationElement ElementFromHandle(IntPtr hwnd);        // 4
    IUIAutomationElement? ElementFromPoint(UiaPoint pt);        // 5
    void _GetFocusedElement();                                  // 6
    void _GetRootElementBuildCache();                           // 7
    void _ElementFromHandleBuildCache();                        // 8
    void _ElementFromPointBuildCache();                         // 9
    void _GetFocusedElementBuildCache();                        // 10
    void _CreateTreeWalker();                                   // 11
    void _get_ControlViewWalker();                              // 12
    void _get_ContentViewWalker();                              // 13
    void _get_RawViewWalker();                                  // 14
    void _get_RawViewCondition();                               // 15
    void _get_ControlViewCondition();                           // 16
    void _get_ContentViewCondition();                           // 17
    void _CreateCacheRequest();                                 // 18
    IUIAutomationCondition CreateTrueCondition();               // 19
    void _CreateFalseCondition();                               // 20
    IUIAutomationCondition CreatePropertyCondition(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value); // 21
}

[ComImport]
[Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElement
{
    void _SetFocus();                                                                  // 1
    void _GetRuntimeId();                                                               // 2
    IUIAutomationElement? FindFirst(TreeScope scope, IUIAutomationCondition condition); // 3
    IUIAutomationElementArray FindAll(TreeScope scope, IUIAutomationCondition condition); // 4
    void _FindFirstBuildCache();                                                        // 5
    void _FindAllBuildCache();                                                          // 6
    void _BuildUpdatedCache();                                                          // 7
    void _GetCurrentPropertyValue();                                                    // 8
    void _GetCurrentPropertyValueEx();                                                  // 9
    void _GetCachedPropertyValue();                                                     // 10
    void _GetCachedPropertyValueEx();                                                   // 11
    void _GetCurrentPatternAs();                                                        // 12
    void _GetCachedPatternAs();                                                         // 13
    [return: MarshalAs(UnmanagedType.IUnknown)] object? GetCurrentPattern(int patternId); // 14
    void _GetCachedPattern();                                                           // 15
    void _GetCachedParent();                                                            // 16
    void _GetCachedChildren();                                                          // 17
    void _get_CurrentProcessId();                                                       // 18
    int CurrentControlType();                                                           // 19
    void _get_CurrentLocalizedControlType();                                            // 20
    [return: MarshalAs(UnmanagedType.BStr)] string CurrentName();                       // 21
    void _get_CurrentAcceleratorKey();                                                  // 22
    void _get_CurrentAccessKey();                                                       // 23
    void _get_CurrentHasKeyboardFocus();                                                // 24
    void _get_CurrentIsKeyboardFocusable();                                             // 25
    void _get_CurrentIsEnabled();                                                       // 26
    [return: MarshalAs(UnmanagedType.BStr)] string CurrentAutomationId();               // 27
}

[ComImport]
[Guid("14314595-b4bc-4055-95f2-58f2e42c9855")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElementArray
{
    int Length();                                    // 1
    IUIAutomationElement GetElement(int index);      // 2
}

[ComImport]
[Guid("352ffba8-0973-437c-a61f-f64cafd81df9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationCondition
{
}

[ComImport]
[Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationValuePattern
{
    void SetValue([MarshalAs(UnmanagedType.BStr)] string value);   // 1
    [return: MarshalAs(UnmanagedType.BStr)] string CurrentValue(); // 2
    int CurrentIsReadOnly();                                        // 3
}

[ComImport]
[Guid("fb377fbe-8ea6-46d5-9c73-6499642d3059")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationInvokePattern
{
    void Invoke();   // 1
}

[ComImport]
[Guid("94cf8058-9b8d-4ab9-8bfd-4cd0a33c8c70")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationTogglePattern
{
    void Toggle();               // 1
    int CurrentToggleState();    // 2  (0=Off, 1=On, 2=Indeterminate)
}

[ComImport]
[Guid("619be086-1f4e-4ee4-bafa-210128738730")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationExpandCollapsePattern
{
    void Expand();                        // 1
    void Collapse();                      // 2
    int CurrentExpandCollapseState();     // 3
}

[ComImport]
[Guid("a8efa66a-0fda-421a-9194-38021f3578ea")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationSelectionItemPattern
{
    void Select();               // 1
    void AddToSelection();       // 2
    void RemoveFromSelection();  // 3
    int CurrentIsSelected();     // 4
}

[Flags]
internal enum TreeScope
{
    Element = 1,
    Children = 2,
    Descendants = 4,
    Subtree = 7,
}

// UIA property and pattern identifiers (UIAutomationClient.h).
internal static class UiaIds
{
    public const int NamePropertyId = 30005;
    public const int ControlTypePropertyId = 30003;
    public const int AutomationIdPropertyId = 30011;

    public const int InvokePatternId = 10000;
    public const int ValuePatternId = 10002;
    public const int ExpandCollapsePatternId = 10005;
    public const int SelectionItemPatternId = 10010;
    public const int TogglePatternId = 10015;
}
