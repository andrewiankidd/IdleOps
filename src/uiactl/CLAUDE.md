# CLAUDE.md — uiactl

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Windows-only CLI for **element-level** desktop automation via UI Automation (UIA).
Where `inpctl` injects raw input and `txtfnd` reads the screen with OCR, `uiactl`
addresses controls by their accessibility tree (AutomationId / Name / ControlType)
and drives them through UIA control patterns — **focus-free, no OCR**, across
WinUI, WPF, UWP, WinForms and Win32.

## Architecture

`uiactl` is a **thin CLI** over `IdleOps.Shared.Windows.Uia` (all UIA logic lives in
`shared`, so `stpcap` reuses the same implementation — no duplicated COM interop).

| Type | Where | Purpose |
|------|-------|---------|
| `UiaAutomation` | shared | The engine: `CUIAutomation` activation, window→element resolution, pattern verbs (`SetValue`/`Invoke`/`Toggle`/`ExpandCollapse`/`Select`), `ElementAt` (point query), `Dump`. Returns public `UiaResult`/`ElementInfo`. |
| `UiaInterop` | shared (internal) | Raw COM vtables for `IUIAutomation` & friends — **no NuGet**. Only the slots we call, `void _slotN()` stubs filling the gaps so ordering matches the native vtable. |
| `ControlTypes` / `ElementInfo` | shared (public) | Control-type id↔name map; element snapshot (type, selectors, patterns, `ClickVerb`). |
| `Options` / `OptionsParser` / `Program` | uiactl | Parse args → build a `Selector` → call `UiaAutomation` → print. Plus `--element-at "x,y"`. |

## Verbs / patterns

| Verb | UIA pattern |
|------|-------------|
| `--set-value` / `--get-value` | ValuePattern |
| `--invoke` | InvokePattern |
| `--toggle` | TogglePattern |
| `--expand` / `--collapse` | ExpandCollapsePattern |
| `--select` | SelectionItemPattern |
| `--dump` | lists elements + their supported patterns (authoring aid) |

## Raw COM notes (important)

- Each declared interface method = one vtable slot, **in order**. Unused methods
  are `void _name()` stubs — never called, so their empty signatures don't matter,
  only their position. If you need a method further down a vtable, add stubs for
  every slot before it.
- `GetCurrentPattern(patternId)` returns `IUnknown`; cast the result to the
  specific pattern interface (RCW does the QueryInterface). Returns null when the
  pattern is unsupported.
- VARIANT params marshal as `[MarshalAs(UnmanagedType.Struct)] object`; BSTR as
  `[MarshalAs(UnmanagedType.BStr)] string`.

## P/Invoke / COM surface

- COM: `CUIAutomation` (CLSID ff48dba4-...), `IUIAutomation`, `IUIAutomationElement`,
  `IUIAutomationElementArray`, `IUIAutomationCondition`, and the Value/Invoke/
  Toggle/ExpandCollapse/SelectionItem pattern interfaces.
- Window resolution reuses `IdleOps.Shared.Windows.WindowMatcher`.

## Dependencies

- NuGet: none (raw COM interop)
- Project: shared
- Platform: Windows-only

## Build & Test

```powershell
dotnet build src/uiactl/uiactl.csproj
```
