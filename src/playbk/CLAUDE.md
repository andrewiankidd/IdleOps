# CLAUDE.md — playbk

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

YAML script execution engine. Reads `.idleops.yaml` scripts that define sequential steps (launch apps, send input, capture media) and executes them. This is the top-level orchestrator for IdleOps workflows.

## Key Types

| Type | Purpose |
|------|---------|
| `Script` | Deserialized YAML root: collection of `Step` objects |
| `Step` | Id (optional), Name, Action (`exec`), Args, Wait (bool) |
| `ScriptRunner` | Loads YAML (UnderscoredNamingConvention), executes steps, manages process PIDs, expands `%id_pid%` tokens |
| `Options` | InputPatterns (glob), OutputDirectory |
| `OptionsParser` | Uses `Microsoft.Extensions.FileSystemGlobbing.Matcher` for input file resolution |

## Script Format

```yaml
steps:
  - id: myapp          # optional — enables %myapp_pid% token in later steps
    name: Launch App
    action: exec        # currently the only action type
    args: notepad.exe
    wait: false         # false = fire-and-forget, true = wait for exit
```

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
