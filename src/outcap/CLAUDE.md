# CLAUDE.md — outcap

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Orchestrates simultaneous audio and video capture, then merges the results into a single MP4 with ffmpeg. Handles time offset synchronization between the two streams using `CaptureResult.StartTimeUtc`.

## Key Types

| Type | Purpose |
|------|---------|
| `CaptureRunner` | Core orchestration: spawns audcap + vidcap via `Task.WhenAll`, calculates `-itsoffset` for sync, merges with ffmpeg, cleans up raw files |
| `Options` | Record with global + per-stream overrides: MergedOutput, KeepRaw, Timer/Delay, nested VideoOptions and AudioOptions |
| `OptionsParser` | Supports global args (`-t`, `-d`) that propagate to streams unless overridden by `--vid-*` / `--aud-*` prefixed args |
| `HelpPrinter` | Custom help renderer (does not use shared HelpPrinter) |

## Merge Logic

1. Audio and video capture run in parallel via `Task.WhenAll`
2. Time offset calculated from `StartTimeUtc` difference between captures
3. ffmpeg merge uses `-itsoffset` for A/V sync correction
4. Handles edge cases: audio-only, video-only, both missing
5. Falls back to copying video if merge fails
6. Deletes raw files unless `--keep-raw`

## Dependencies

- NuGet: YamlDotNet 15.3.0, Microsoft.Extensions.FileSystemGlobbing 8.0.0
- Project: shared, audcap, vidcap
- External: ffmpeg on PATH

## Build & Test

```powershell
dotnet build src/outcap/outcap.csproj
dotnet test src/outcap.Tests/outcap.Tests.csproj
```
