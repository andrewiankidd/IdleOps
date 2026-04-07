# Script Authoring Guide

playbk executes `.idleops.yaml` scripts that define step-by-step desktop automation workflows.

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

### `screenshot` — capture a window to file

```yaml
- name: Screenshot the app
  action: screenshot
  window: "Notepad*"
  output: screenshots/notepad.png
```

## Fields

| Field | Used by | Description |
|-------|---------|-------------|
| `name` | All | Display name shown in console during execution |
| `action` | All | Action type: `exec`, `sleep`, `wait-window`, `click-text`, `screenshot` |
| `id` | `exec` | Identifier. Enables `%id_pid%` token expansion in later steps |
| `args` | `exec`, `sleep` | Command string (exec) or duration in seconds (sleep) |
| `wait` | `exec` | If `true`, block until process exits. Default: `false` |
| `window` | `wait-window`, `click-text`, `screenshot` | Window title pattern (supports `*` wildcards) |
| `text` | `click-text` | Text to find via OCR (case-insensitive substring) |
| `output` | `screenshot` | Output file path (PNG, JPEG, BMP) |
| `timeout` | `wait-window`, `sleep` | Timeout in seconds |

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
