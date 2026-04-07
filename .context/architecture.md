# Architecture

## Solution Structure

```
IdleOps.sln
├── src/
│   ├── shared/              Class library — platform, logging, CLI, windows, capture
│   ├── audcap/              CLI — system audio capture
│   ├── vidcap/              CLI — screen/window video capture
│   ├── outcap/              CLI — A/V orchestration and merge
│   ├── playbk/              CLI — YAML script execution engine
│   ├── inpctl/              CLI — keyboard/mouse input + window management
│   ├── txtfnd/              CLI — OCR text finder (Windows)
│   ├── scrcap/              CLI — screenshot capture (Windows)
│   ├── waitfr/              CLI — wait for window/text conditions (Windows)
│   ├── imgfnd/              CLI — image template matching (Windows)
│   ├── stpcap/              CLI — input recorder (Windows)
│   ├── spkbak/              CLI — text-to-speech (Windows)
│   ├── cnvrtr/              CLI — universal converter
│   ├── *.Tests/             13 test projects (one per tool/library)
├── CLAUDE.md
├── README.md
├── docs/
└── .context/
```

## Dependency Graph

```
playbk ──► outcap ──► audcap ──► shared
  │          │                      ▲
  │          └──► vidcap ───────────┤
  │                                 │
  ├──► inpctl ──────────────────────┤
  ├──► txtfnd ──────────────────────┤
  └──► scrcap ──────────────────────┘

waitfr ──► shared          (standalone)
imgfnd ──► shared          (standalone + OpenCvSharp4)
stpcap ──► shared          (standalone)
spkbak                     (no dependencies)
cnvrtr                     (no dependencies)
```

### Build-time Wiring

playbk's `.csproj` has custom MSBuild `AfterTargets="Build"` targets that copy outcap, inpctl, txtfnd, and scrcap binaries into playbk's output directory. At runtime, playbk prepends `AppContext.BaseDirectory` to PATH so scripts can invoke these tools without absolute paths.

## Core Namespace Map

| Namespace | Project | Key Types |
|-----------|---------|-----------|
| `IdleOps.Shared.Platform` | shared | `HostPlatform`, `HostInfo` |
| `IdleOps.Shared.Logging` | shared | `ConsoleLogger` |
| `IdleOps.Shared.Cli` | shared | `HelpContent`, `HelpPrinter`, `ArgParser` |
| `IdleOps.Shared.Capture` | shared | `CaptureResult` |
| `IdleOps.Shared.Windows` | shared | `WindowMatcher`, `WindowCapture`, `WindowInfo`, `RECT`, `NativeMethods` |
| `audcap` | audcap | `IAudioCapturer`, `AudioCapturerFactory`, `AudcapService` |
| `vidcap` | vidcap | `IVideoCapturer`, `VideoCapturerFactory`, `VidcapService` |
| `outcap` | outcap | `CaptureRunner`, `Options`, `OptionsParser` |
| `playbk.Execution` | playbk | `ScriptRunner` (exec, sleep, wait-window, click-text, screenshot) |
| `playbk.Model` | playbk | `Script`, `Step` |
| `inpctl.Cli` | inpctl | `Options`, `OptionsParser`, `HelpFactory` |
| `inpctl.Input` | inpctl | `InputSender`, `Interop` (INPUT, MOUSEINPUT structs) |
| `txtfnd.Ocr` | txtfnd | `OcrService`, `OcrTextResult` |
| `stpcap.Recording` | stpcap | `InputRecorder`, `ScriptGenerator`, `InputEvent` |

## Target Frameworks

| TFM | Projects |
|-----|----------|
| `net10.0` | shared, audcap, vidcap, outcap, playbk, inpctl, scrcap, stpcap, cnvrtr, imgfnd |
| `net10.0-windows10.0.22621.0` | txtfnd, waitfr, spkbak (WinRT APIs: OCR, TTS) |
