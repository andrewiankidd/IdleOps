# uiactl — Element Automation via UI Automation

> **Platform:** 🟢 Windows · 🟡 Linux · 🟡 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Drive desktop controls by their **accessibility tree** instead of screen
coordinates or OCR. `uiactl` finds a control by AutomationId / Name / ControlType
and operates it through UI Automation control patterns — **focus-free, no OCR,
DPI-independent** — across WinUI, WPF, UWP, WinForms and Win32. Windows only.

This is the most robust of IdleOps' three automation tiers:

| Tier | Tool | Best for |
|------|------|----------|
| Element | **uiactl** | Accessible apps (WinUI/WPF/UWP/WinForms/Win32). Focus-free |
| Visual | txtfnd + inpctl | Non-accessible UIs: games, canvas, custom-drawn |
| Low-level | inpctl | Raw keystrokes/chords, anything on screen |

## Usage

```bash
# discover elements + their automation-ids and supported patterns
uiactl -w "*Notepad" --dump

# set a field's value with no focus needed
uiactl -w "My App*" --automation-id "UrlField" --set-value "http://127.0.0.1:8737"

# read a value back
uiactl -w "My App*" --automation-id "StatusField" --get-value

# click a button / menu item by its label
uiactl -w "My App*" --name "Don't save" --invoke

# toggle a checkbox, select a list item, expand a menu
uiactl -w "My App*" --name "Enable telemetry" --toggle
uiactl -w "My App*" --name "Dark" --select
uiactl -w "My App*" --automation-id "FileMenu" --expand
```

## Selectors

Pick **one** (AutomationId wins, then Name, then ControlType):

| Flag | Matches |
|------|---------|
| `--automation-id <id>` | UIA AutomationId (most stable — prefer this) |
| `--name <name>` | Accessibility Name (e.g. a button's label) |
| `--control-type <type>` | Control type: `Edit`, `Button`, `Document`, ... or a numeric UIA id |

## Verbs

| Flag | Pattern | Use for |
|------|---------|---------|
| `--set-value <text>` | Value | Text fields, editors |
| `--get-value` | Value | Read a value (prints to stdout) |
| `--invoke` | Invoke | Buttons, menu items |
| `--toggle` | Toggle | Checkboxes, toggle buttons |
| `--expand` / `--collapse` | ExpandCollapse | Menus, combos, tree items |
| `--select` | SelectionItem | List items, tabs, radios |
| `--dump` | — | List elements under the window (add `--max <n>`) |

## Notes

- Elements must be **accessible** (implement the relevant UIA pattern). Custom-drawn
  UIs (many games) expose no tree — use `txtfnd`/`inpctl` there.
- `--set-value` replaces the whole value and requires a writable `ValuePattern`.
  For keystroke-level input or key chords, use `inpctl`.
- No NuGet dependencies — implemented with raw `IUIAutomation` COM interop.

See the [Script Authoring Guide](../scripting.md) for the matching playbk actions
(`set-value`, `assert-value`, `invoke`, `toggle`, `expand`, `collapse`, `select`).
