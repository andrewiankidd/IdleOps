# CLAUDE.md — playbk

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

YAML script execution engine. Reads `.idleops.yaml` scripts that define sequential steps (launch apps, send input, capture media) and executes them. This is the top-level orchestrator for IdleOps workflows.

## Key Types

| Type | Purpose |
|------|---------|
| `Script` | Deserialized YAML root: collection of `Step` objects |
| `Step` | Id (optional), Name, Action, Args, Wait, plus Window/Text/Output/Image/Voice/Timeout for built-in actions |
| `ScriptRunner` | Loads YAML (UnderscoredNamingConvention), executes steps, manages process PIDs, expands `%id_pid%` tokens |
| `Options` | InputPatterns (glob), OutputDirectory |
| `OptionsParser` | Uses `Microsoft.Extensions.FileSystemGlobbing.Matcher` for input file resolution |

## Script Format

```yaml
steps:
  - id: myapp          # optional — enables %myapp_pid% token in later steps
    name: Launch App
    action: exec        # exec | sleep | wait-window | click-text | assert-text | type | keyboard | screenshot | speak | hold | UIA verbs
    # keyboard: send a chord/sequence via inpctl, e.g. text: "CTRL+S" or "CTRL+A, DELETE" (aliases: keys, chord)
    args: notepad.exe
    wait: false         # false = fire-and-forget, true = wait for exit
    retries: 0          # extra attempts after first failure (Ansible/ADO style); retry_delay: seconds between
    continue_on_error: false  # if true, log failure and keep going instead of halting
```

Retry/error-handling fields (`retries`, `retry_delay`, `continue_on_error`) apply to
**any** action and are handled centrally in `RunStepWithRetryAsync` wrapping
`DispatchStepAsync` — individual `RunXAsync` handlers stay retry-agnostic.

## Device profiles + static validation

`--profile local` (default) | `offbox`. A `DeviceProfile` grants a set of
`Capability` flags (LocalProcess / Input / Vision / WindowHandle / Uia); each action
declares what it needs (`RunbookValidator.RequiredCapabilities`). Before any step
runs, `RunbookValidator.Validate` rejects the whole runbook (loudly, listing every
offending step) if a step needs a capability the profile lacks — so a bad
transport/action combination fails pre-flight instead of mid-run.

- **local**: everything (SendInput + window capture + UI Automation).
- **offbox**: drive another machine over USB-HID (input) + capture card (vision).
  Vision-only: no `exec` (no process control on the target), no UIA verbs, no
  window-title matching or `--background` delivery (no HWND). Use `click-text` /
  `keyboard` / text-based `wait-window`. The HID sink + capture source transports
  are not built yet; the profile + validation define the contract they'll satisfy.

## Execution Details

- Prepends `AppContext.BaseDirectory` to PATH so outcap/inpctl are discoverable
- `PLAYBK_CAPTURE_TIMER` env var controls default capture duration (default: 10s)
- `%id_pid%` token expansion: if a step has `id: foo`, later steps can use `%foo_pid%` in args
- Process resolution: checks rooted paths, PATH, Windows Registry App Paths
- Cross-platform: wraps commands in `cmd.exe /c` on Windows, `sh -c` on Unix
- MSBuild targets copy outcap and inpctl binaries into playbk output dir at build time

## Dependencies

- NuGet: YamlDotNet 15.3.0, Microsoft.Extensions.FileSystemGlobbing 8.0.0
- Project: shared, outcap, inpctl
- External: ffmpeg on PATH (transitive via outcap)

## Build & Test

```powershell
dotnet build src/playbk/playbk.csproj
dotnet test src/playbk.Tests/playbk.Tests.csproj
```

## Example Scripts

- `inputs/rickroll.idleops.yaml` — Opens YouTube, records 10s with A/V
- `inputs/notepad-hello-world.idleops.yaml` — Launches Notepad, types text, records
- `inputs/mspaint-smiley.idleops.yaml` — Launches Paint, draws smiley with percentage-based mouse coords
