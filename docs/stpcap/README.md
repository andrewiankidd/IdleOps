# stpcap — Input Recorder

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
- **Mouse**: Clicks with window-relative coordinates, drags as `x1,y1-x2,y2`
- **Timing**: Gaps > 500ms between actions become `sleep` steps

## Output Format

The generated YAML uses `exec` steps calling inpctl, with `sleep` steps for timing:

```yaml
steps:
  - name: Type text
    action: exec
    args: inpctl --window "Notepad*" --type "hello world"
    wait: true

  - name: Wait 2s
    action: sleep
    args: "2"

  - name: Click at (100,200)
    action: exec
    args: inpctl --window "Notepad*" --leftmouse "100,200"
    wait: true
```
