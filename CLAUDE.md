# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is IdleOps?

IdleOps is a modular .NET CLI toolkit for automating desktop interactions, capturing media, and replaying scripted workflows. Think Cypress/Playwright-style config-driven automation, but for the whole desktop — including text-to-speech, video/screen capture, audio capture, and keyboard/mouse input simulation.

## Build and Run

```powershell
# restore and build the entire solution
dotnet build IdleOps.sln

# run a specific tool
dotnet run --project src/audcap -- --help
dotnet run --project src/vidcap -- --help
dotnet run --project src/outcap -- --help
dotnet run --project src/playbk -- --help
dotnet run --project src/inpctl -- --help

# run all tests
dotnet test IdleOps.sln

# run a single test project
dotnet test src/audcap.Tests/audcap.Tests.csproj

# run a single test by name
dotnet test src/audcap.Tests/audcap.Tests.csproj --filter "CapturesSystemAudioForFiveSeconds"
```

## Tech Stack

- .NET 10.0 (all projects), C# with nullable reference types and implicit usings
- xUnit 2.7.0 + coverlet for testing and coverage
- NAudio 2.2.1 (Windows audio via WASAPI loopback)
- YamlDotNet 15.3.0 (script parsing in playbk/outcap)
- Microsoft.Extensions.FileSystemGlobbing 8.0.0 (input pattern matching)
- ffmpeg/ffprobe required on PATH for video capture, audio capture on non-Windows, and A/V merge

## Architecture

### Overview

Eight CLI tools built around a shared library. Each tool is a standalone executable that can be used independently or composed via playbk scripts. The dependency graph flows upward: `shared` is the base, individual capture tools build on it, `outcap` orchestrates capture tools, and `playbk` orchestrates everything.

### Projects

| Project | Type | Description | CLAUDE.md |
|---------|------|-------------|-----------|
| shared | Class library (net10.0) | Cross-cutting utilities (logging, platform detection, CLI help, capture results, window matching, screenshot capture, **UI Automation**) | [src/shared/CLAUDE.md](src/shared/CLAUDE.md) |
| shared.win | Class library (net10.0-windows) | WinRT-dependent shared logic — **OCR** (`WindowTextFinder`, warm engine). Separate TFM so `shared` stays cross-platform | — |
| audcap | CLI executable | Cross-platform system audio capture (WASAPI on Windows, ffmpeg on macOS/Linux) | [src/audcap/CLAUDE.md](src/audcap/CLAUDE.md) |
| vidcap | CLI executable | Cross-platform screen/window video capture via ffmpeg | [src/vidcap/CLAUDE.md](src/vidcap/CLAUDE.md) |
| outcap | CLI executable | Orchestrates audcap + vidcap in parallel, merges with ffmpeg | [src/outcap/CLAUDE.md](src/outcap/CLAUDE.md) |
| playbk | CLI executable (net10.0-windows) | YAML script engine — runs steps, manages processes, records output. Calls OCR/UIA/input **in-process** (thin shells over shared) | [src/playbk/CLAUDE.md](src/playbk/CLAUDE.md) |
| inpctl | CLI executable | Windows-only keyboard/mouse input via P/Invoke (SendInput, PostMessage for background) | [src/inpctl/CLAUDE.md](src/inpctl/CLAUDE.md) |
| uiactl | CLI executable | Windows-only element automation via UI Automation (thin CLI over `shared`) | [src/uiactl/CLAUDE.md](src/uiactl/CLAUDE.md) |
| txtfnd | CLI executable | Windows-only OCR text finder — thin CLI over `shared.win` | [src/txtfnd/CLAUDE.md](src/txtfnd/CLAUDE.md) |
| scrcap | CLI executable | Windows-only screenshot capture — saves a window to PNG/JPEG/BMP | [src/scrcap/CLAUDE.md](src/scrcap/CLAUDE.md) |
| stpcap | CLI executable | Windows-only input recorder — emits semantic steps (UIA → OCR → coords) | — |

### Dependency Graph

```
playbk ──► outcap ──► audcap ──► shared
  │          │                      ▲
  │          └──► vidcap ───────────┘
  │
  ├──► shared.win ──► shared     (in-process OCR, warm engine)
  ├──► inpctl ──► shared
  ├──► uiactl ──► shared         (UI Automation)
  ├──► txtfnd ──► shared.win
  └──► scrcap ──► shared
```

`shared` stays net10.0 (cross-platform for audcap/vidcap); `shared.win` is net10.0-windows
(WinRT OCR). playbk is net10.0-windows so it can link OCR/UIA in-process rather than
shelling out. Its `.csproj` still copies outcap/inpctl/uiactl/txtfnd/scrcap binaries into
its output so `exec` steps and the CLIs remain available on PATH.

### Key Patterns

- **Platform abstraction via factories**: Each capture tool has an `IAudioCapturer`/`IVideoCapturer` interface with platform-specific implementations selected by a factory using `RuntimeInformation.IsOSPlatform`.
- **Records for options, classes for services**: CLI options and results are immutable records; stateful services are classes.
- **Manual CLI parsing**: Each tool rolls its own `OptionsParser` — no third-party CLI framework. Arguments follow `-short` / `--long` conventions.
- **Process lifecycle management**: ffmpeg processes are started as subprocesses and gracefully stopped via stdin `q` command with a 3-second timeout before kill.
- **YAML script model**: playbk uses YamlDotNet with `UnderscoredNamingConvention` (snake_case YAML maps to PascalCase C#). Scripts define steps with `action: exec` and support `%id_pid%` token expansion.
- **Async throughout**: All capture operations are `Task`-based with `CancellationToken` threading. Processes use `WaitForExitAsync`.
- **Namespaces by tool**: Tool-specific code uses flat namespaces (`audcap`, `vidcap`, etc.). Shared code uses `IdleOps.Shared.*` with sub-namespaces by concern.

## Deep Context

See [.context/README.md](.context/README.md) for detailed architecture and technical documentation:

| File | What it covers |
|------|---------------|
| [overview.md](.context/overview.md) | Project purpose, current status, tech stack |
| [architecture.md](.context/architecture.md) | Solution structure, dependency graph, namespace map |
| [conventions.md](.context/conventions.md) | Code patterns, async conventions, error handling, naming |
| [extension-points.md](.context/extension-points.md) | How to add new capture backends, script actions, platforms |
| [external-tools.md](.context/external-tools.md) | ffmpeg/ffprobe invocations and platform-specific details |
| [tech-debt.md](.context/tech-debt.md) | Known technical debt and areas for improvement |
| [recommendations.md](.context/recommendations.md) | Quick fixes and future roadmap |

## Documentation

See [docs/README.md](docs/README.md) for user-facing documentation:

- [Setup & Prerequisites](docs/setup.md)
- [Script Authoring Guide](docs/scripting.md)
- [Per-tool guides](docs/README.md#per-tool-guides)
