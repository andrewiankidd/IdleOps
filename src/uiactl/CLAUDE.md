# CLAUDE.md — uiactl

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform CLI for **element-level** desktop automation by accessibility tree.
Where `inpctl` injects raw input and `txtfnd` reads the screen with OCR, `uiactl`
addresses controls by their accessibility tree (AutomationId / Name / ControlType)
and drives them — **focus-free, no OCR**. Windows uses UI Automation (COM); Linux
uses AT-SPI2 (via a bundled pyatspi helper).

## Architecture

Program picks an `IUiAutomation` via `UiAutomationFactory` (mirrors the other tools'
platform factories):

| Backend | Where | How |
|---------|-------|-----|
| `WindowsUiAutomation` | uiactl | Delegates to `IdleOps.Shared.Windows.Uia.UiaAutomation` (COM UIA), shared with `stpcap`. |
| `LinuxUiAutomation` | uiactl | Shells out to `atspi_helper.py` (pyatspi); maps the UIA selector (name + control type) onto AT-SPI names/roles. |

The engine logic still lives in `IdleOps.Shared.Windows.Uia` for the Windows path:

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

## Linux (AT-SPI2) backend

- `atspi_helper.py` (copied beside the binary) uses **pyatspi**: `apt install python3-pyatspi at-spi2-core`, plus a running accessibility bus (`GTK_MODULES=gail:atk-bridge`, a session D-Bus). App coverage varies — GTK/GNOME apps expose rich trees; some toolkits expose little.
- Selector mapping: `--name` → accessible name; `--control-type` → AT-SPI role (e.g. Button→"push button", Edit→"text") via `LinuxUiAutomation.RoleMap`. AT-SPI rarely exposes an AutomationId, so `--automation-id` falls back to name.
- Verbs → AT-SPI: invoke/toggle/select/expand/collapse → the Action interface (`doAction`); get/set-value → Value / EditableText / Text interfaces; dump → tree walk; element-at → `getAccessibleAtPoint`.
- Verified end-to-end against gnome-calculator under Xvfb (`scripts/linux-uiactl-e2e.sh`, wired into CI).

## Dependencies

- NuGet: none (raw COM interop on Windows)
- Project: shared
- Platform: **Windows** 🟢 UIA · **Linux** 🟢 AT-SPI2 (needs python3-pyatspi + a11y bus) · **macOS** 🟡 (AXUIElement not wired up)

## Build & Test

```powershell
dotnet build src/uiactl/uiactl.csproj
```
