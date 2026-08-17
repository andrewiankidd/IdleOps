# stpcap — Input Recorder

> **Platform:** 🟢 Windows · 🟢 Linux (X11) · 🟡 macOS  —  🟢 works · 🟡 partial · 🔴 not available
>
> **Linux (X11):** records via the XRecord extension (needs `python3-xlib`). Clicks are recorded as **window-relative coordinates**, not semantic UIA steps — the Windows recorder uses UIA element-at to emit resilient `invoke`/`click-text` steps; AT-SPI element enrichment on Linux isn't wired up yet. X11 only (no Wayland). Stop with Ctrl+C.
> **macOS:** no recorder backend yet — exits with a clear message.

Record keyboard and mouse input into an IdleOps YAML script. The inverse of playbk — perform actions once, then replay them.

## Usage

```bash
# record all input, save to script
dotnet run --project src/stpcap -- -o my-workflow.idleops.yaml

# only record input targeting specific windows
dotnet run --project src/stpcap -- --window "My App*" -o my-workflow.idleops.yaml
```

Press Ctrl+C to stop recording and save.

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-o, --output` | Output YAML file | `recorded.idleops.yaml` |
| `-w, --window` | Only capture events for matching windows | all windows |

## What Gets Recorded

- **Keyboard**: Key presses coalesced into `--type` for text input, `--keyboard` for key combos
- **Mouse (left click)**: recorded as **resilient semantic steps** where possible —
  at click time stpcap queries UI Automation for the control under the cursor and emits:
  1. a semantic action (`invoke` / `select` / `toggle` / `expand`) addressed by
     `automation_id` (or `element` name) — self-healing, survives layout/position changes;
  2. otherwise an OCR `click-text` by the control's visible label;
  3. otherwise raw window-relative coordinates (the last-resort fallback).
- **Mouse (right/middle, drags)**: window-relative coordinates
- **Timing**: Gaps > 500ms between actions become `sleep` steps

This makes recordings far less brittle than coordinate macros: a button that moves
still resolves by its AutomationId. Non-accessible UIs (games, custom-drawn) fall
through to OCR/coordinates automatically.

## Output Format

A left click on an accessible control records a semantic step:

```yaml
steps:
  - name: Type text
    action: exec
    args: inpctl --window "My App*" --type "hello world"
    wait: true

  - name: Invoke SaveButton
    action: invoke
    window: "My App*"
    automation_id: "SaveButton"

  - name: Wait 2s
    action: sleep
    args: "2"
```

When no accessible element is found, it falls back to the classic coordinate step
(`action: exec` → `inpctl --leftmouse "x,y"`).
