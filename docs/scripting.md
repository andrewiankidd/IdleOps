# Script Authoring Guide

playbk executes `.idleops.yaml` scripts that define step-by-step desktop automation workflows.

## Editor validation (schema)

A JSON Schema for runbooks lives at [`schema/idleops.schema.json`](../schema/idleops.schema.json).
It gives autocomplete, action-enum validation, and per-action field hints in any
editor with YAML language-server support (VS Code + the Red Hat YAML extension,
JetBrains, etc.).

Inside this repo it applies automatically (see `.vscode/settings.json`). In **any
other repo**, add a modeline to the top of the file so the editor links back to
the canonical schema on `main`:

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/andrewiankidd/IdleOps/main/schema/idleops.schema.json
steps:
  - name: ...
```

Use the **raw** URL above — a `github.com/.../blob/...` link serves HTML and won't
parse. It tracks `main`, so it reflects the latest contract rather than any
specific installed build.

## Actions

### `exec` — run a command

```yaml
- name: Launch Notepad
  action: exec
  args: notepad.exe
  wait: false         # true = block until exit, false = fire and forget
```

### `sleep` — delay execution

```yaml
- name: Wait 2 seconds
  action: sleep
  args: "2"
```

### `wait-window` — wait for a window to appear

```yaml
- name: Wait for Notepad
  action: wait-window
  window: "*Notepad*"
  timeout: 10           # seconds, default 10
```

### `click-text` — find text via OCR and click it

```yaml
- name: Click File menu
  action: click-text
  window: "Notepad*"
  text: "File"
```

### `assert-text` — verify text is present via OCR (no click)

The read-only sibling of `click-text`: finds the text but doesn't click it, and
**fails the step (stopping the flow) when the text is absent** — the primitive for
asserting UI state. If the text needs time to appear, `wait-window` with a `text:`
first, then `assert-text`.

```yaml
- name: Verify we're connected
  action: assert-text
  window: "My App*"
  text: "Connected"
```

### `type` — type text into a window

First-class alternative to `exec inpctl --type`. The target field must already
have focus (pair with a `click-text` on it first if needed).

```yaml
- name: Fill the URL field
  action: type
  window: "My App*"
  text: "http://example.com"
```

By default `type` foregrounds the target window and injects via `SendInput` — the
reliable path that also drives Chromium/WebView2 (Tauri) content. This briefly
takes focus and the cursor.

Set `background: true` to post keystrokes to the window **without** stealing focus
or the cursor — useful when you're driving one app while working in another (e.g.
scripting a window while recording a demo). Caveats: it's classic-Win32 only (it
posts `WM_CHAR` to the focused child control), so it does **not** work for
webviews, and if the target window has no focused control the input may not land.

```yaml
- name: Type into a background window (no focus steal)
  action: type
  window: "Some Editor*"
  text: "hello"
  background: true
```

### `screenshot` — capture a window to file

```yaml
- name: Screenshot the app
  action: screenshot
  window: "Notepad*"
  output: screenshots/notepad.png
```

### UI Automation actions — drive controls by accessibility (uiactl)

`click-text`/`type` work visually (OCR + simulated input) and need the target
on-screen and focused. The **UI Automation** actions instead address controls by
their accessibility tree (`automation_id` / `element` name / `control_type`) and
drive them through UIA control patterns — **focus-free, no OCR, DPI-independent**,
and they work on WinUI/WPF/UWP/WinForms/Win32 alike (including the modern Store
Notepad that ignores simulated `type`). Windows only.

Pick **one** selector per step: `automation_id` (preferred), else `element` (the
accessibility Name), else `control_type` (e.g. `Edit`, `Button`, `Document`).
Discover them with `uiactl -w "<title>" --dump`.

| Action | What it does | Pattern |
|--------|--------------|---------|
| `set-value` | Set the element's value to `text` | Value |
| `assert-value` | Read the value; fail unless it equals `text` | Value |
| `invoke` | Invoke a button / menu item | Invoke |
| `toggle` | Toggle a checkbox / toggle button | Toggle |
| `expand` / `collapse` | Expand/collapse a menu / combo / tree item | ExpandCollapse |
| `select` | Select a list item / tab / radio | SelectionItem |

```yaml
- name: Set the URL field (no focus needed)
  action: set-value
  window: "My App*"
  automation_id: "UrlField"
  text: "http://127.0.0.1:8737"

- name: Click Connect
  action: invoke
  window: "My App*"
  element: "Connect"

- name: Verify it connected
  action: assert-value
  window: "My App*"
  automation_id: "StatusField"
  text: "Connected"
```

## Retries and error handling

By default a step that fails halts the whole run (fail fast). Any step can opt
into Ansible / AzureDevOps-style retry and error-tolerance:

```yaml
- name: Wait for the app to finish loading
  action: assert-text
  window: "My App*"
  text: "Connected"
  retries: 4           # up to 5 total attempts (1 + 4)
  retry_delay: 2       # seconds between attempts (default 0)

- name: Best-effort screenshot (never fails the run)
  action: screenshot
  window: "My App*"
  output: docs/optional.png
  continue_on_error: true
```

- **`retries`** — extra attempts after the first failure. `retries: 2` means up
  to 3 total attempts. Default `0` (fail fast, the original behaviour).
- **`retry_delay`** — seconds to wait between attempts. Default `0`.
- **`continue_on_error`** — if the step still fails after its retries, log it and
  continue the run instead of halting. (Ansible `ignore_errors` / ADO
  `continueOnError`.)

Retries wrap the whole step, so they compose with any action — a flaky launch
(`exec`), an element that appears late (`click-text`/`assert-text`), etc. For
"wait until a window exists" prefer `wait-window` (it polls with a single
`timeout`); reach for `retries` when you want to re-run the *action itself*.

> Retried steps re-run verbatim, so retry side-effecting steps (like `exec`
> launching an app) only when a partial run is safe to repeat.

## Fields

| Field | Used by | Description |
|-------|---------|-------------|
| `name` | All | Display name shown in console during execution |
| `action` | All | `exec`, `sleep`, `wait-window`, `click-text`, `assert-text`, `type`, `screenshot`, `speak`, and UIA: `set-value`, `assert-value`, `invoke`, `toggle`, `expand`, `collapse`, `select` |
| `id` | `exec` | Identifier. Enables `%id_pid%` token expansion in later steps |
| `args` | `exec`, `sleep` | Command string (exec) or duration in seconds (sleep) |
| `wait` | `exec` | If `true`, block until process exits. Default: `false` |
| `window` | `wait-window`, `click-text`, `assert-text`, `type`, `screenshot` | Window title pattern (supports `*` wildcards) |
| `text` | `click-text`, `assert-text`, `type` | Text to find via OCR (`click-text`/`assert-text`) or to type (`type`) |
| `output` | `screenshot` | Output file path (PNG, JPEG, BMP) |
| `timeout` | `wait-window`, `sleep` | Timeout in seconds |
| `retries` | All | Extra attempts after the first failure. Default `0` |
| `retry_delay` | All | Seconds between retry attempts. Default `0` |
| `continue_on_error` | All | If `true`, keep running after this step fails. Default `false` |
| `background` | `type` | Post input without foregrounding the window (Win32 only, not webviews). Default `false` |
| `text` (value) | `set-value`, `assert-value` | The value to set / the expected value |
| `automation_id` | UIA actions | Select the element by AutomationId (preferred) |
| `element` | UIA actions | Select the element by accessibility Name |
| `control_type` | UIA actions | Select the element by control type (`Edit`, `Button`, `Document`, ...) |

## Token Expansion

When a step has an `id`, its process ID becomes available as `%id_pid%` in subsequent `exec` steps:

```yaml
steps:
  - id: recorder
    name: Start recording
    action: exec
    args: outcap.exe --timer 30
    wait: false

  - name: Stop recording
    action: exec
    args: inpctl --pid %recorder_pid% --ctrlc
    wait: true
```

## Examples

### Automated screenshot generation

```yaml
steps:
  - name: Launch app
    action: exec
    args: dotnet run --project C:\git\myapp\src\MyApp.Gui
    wait: false

  - name: Wait for app
    action: wait-window
    window: "My App*"
    timeout: 15

  - name: Let it load
    action: sleep
    args: "3"

  - name: Click Settings tab
    action: click-text
    window: "My App*"
    text: "Settings"

  - name: Wait for tab
    action: sleep
    args: "1"

  - name: Screenshot settings
    action: screenshot
    window: "My App*"
    output: docs/screenshots/settings.png

  - name: Close app
    action: exec
    args: inpctl --window "My App*" --keyboard "ALT+F4"
    wait: true
```

### Record a video demo

```yaml
steps:
  - name: Open YouTube
    action: exec
    args: chrome.exe https://www.youtube.com/watch?v=dQw4w9WgXcQ
    wait: false

  - name: Wait for browser
    action: wait-window
    window: "Rick Astley*"
    timeout: 15

  - name: Let video load
    action: sleep
    args: "3"

  - name: Record 10 seconds
    action: exec
    args: outcap.exe --window "Rick Astley*" --timer 10
    wait: true

  - name: Close browser
    action: exec
    args: inpctl --window "Rick Astley*" --keyboard "ALT+F4"
    wait: true
```

## Process Resolution

Commands in `exec` args are resolved in this order:
1. Absolute/rooted paths (used as-is)
2. PATH environment variable lookup (playbk adds its own bin dir to PATH)
3. Windows Registry App Paths (Windows only)

On Windows, commands are wrapped in `cmd.exe /c`. On Unix, they use `sh -c`.
