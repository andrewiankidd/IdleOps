# IdleOps

IdleOps is a modular toolkit for automating desktop interactions, capturing media, and replaying scripted workflows. Think Cypress/Playwright-style config-driven steps, but for the whole desktop — native apps, system audio, screen recording, keyboard/mouse simulation, and more.

## Components

Platform support: 🟢 works · 🟡 stubbed (builds, but exits with a clear "not implemented on this platform" message — no native equivalent yet) · 🔴 not available (Windows-only build).

### Capture
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[audcap](docs/audcap/README.md)** | Cross-platform system audio capture (WAV) | 🟢 | 🟢 | 🟢 |
| **[vidcap](docs/vidcap/README.md)** | Cross-platform screen/window video capture (MP4) | 🟢 | 🟢 | 🟢 |
| **[outcap](docs/outcap/README.md)** | Synchronized audio + video capture and merge | 🟢 | 🟢 | 🟢 |
| **[scrcap](docs/scrcap/README.md)** | Window screenshot capture (PNG/JPEG/BMP) | 🟢 | 🟡 | 🟡 |

### Automation
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[playbk](docs/playbk/README.md)** | YAML script execution engine (exec, sleep, wait-window, click-text, assert-text, type, screenshot, speak, UIA verbs) | 🟢 | 🔴 | 🔴 |
| **[inpctl](docs/inpctl/README.md)** | Keyboard/mouse input + window management | 🟢 | 🟡 | 🟡 |
| **[uiactl](docs/uiactl/README.md)** | Element automation via UI Automation — set-value/invoke/toggle/etc. by accessibility tree, focus-free | 🟢 | 🟡 | 🟡 |
| **[txtfnd](docs/txtfnd/README.md)** | Find text on screen via OCR, return coordinates | 🟢 | 🔴 | 🔴 |
| **[imgfnd](docs/imgfnd/README.md)** | Find UI elements by reference image | 🟢 | 🟡 | 🟡 |
| **[waitfr](docs/waitfr/README.md)** | Wait for window/text conditions | 🟢 | 🔴 | 🔴 |
| **[stpcap](docs/stpcap/README.md)** | Record user input into YAML scripts — emits resilient semantic steps (UIA → OCR → coords) | 🟢 | 🟡 | 🟡 |

### Utilities
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[spkbak](docs/spkbak/README.md)** | Text-to-speech — WinRT on Windows, `say`/`espeak` on macOS/Linux | 🟢 | 🟢 | 🟢 |
| **[cnvrtr](docs/cnvrtr/README.md)** | Universal converter — encodings, units, dates, files (200+ formats) | 🟢 | 🟢 | 🟢 |

## Quick Start

```bash
# build everything
dotnet build IdleOps.sln

# run the rickroll demo (opens YouTube, records 10s of audio+video)
dotnet run --project src/playbk -- -i src/playbk/inputs/rickroll.idleops.yaml -o ./outputs

# capture 5 seconds of audio
dotnet run --project src/audcap -- -t 5 -o demo.wav

# capture 10 seconds of video
dotnet run --project src/vidcap -- -t 10 -o demo.mp4

# type into a window
dotnet run --project src/inpctl -- --window "Notepad*" --type "Hello from IdleOps!"

# find text on screen and get its coordinates (for clicking via inpctl)
dotnet run --project src/txtfnd -- --window "Notepad*" --text "File"
```

## Use Cases

### Automated screenshot generation for documentation
Define a `.idleops.yaml` script that launches your app, navigates the UI via OCR, captures screenshots, and saves them to your repo's docs folder. Re-run the script whenever the UI changes to refresh all screenshots in one go.

> **Real-world examples**:
> - **[Crosspose](https://github.com/andrewiankidd/Crosspose)** — **15 screenshots**: full sidebar navigation, container details tabs, dark/light mode variants, and per-GUI screens. Scripts live in the Crosspose repo at [`assets/idleops/`](https://github.com/andrewiankidd/Crosspose/tree/main/assets/idleops) (`crosspose-gui-screenshots.idleops.yaml` et al).
> - **POSEIDEN** — GUI screenshots for a Rust/Tauri desktop app's docs, driven against an anonymized demo instance (`poseiden-gui-screenshots.idleops.yaml`).
>
> Both keep their `.idleops.yaml` scripts in their own repos and link back to this project's [schema](schema/idleops.schema.json) via a `# yaml-language-server: $schema=…` modeline.

### Automated video documentation / demo recording
Open an app, perform a workflow (type, click, navigate menus), record both screen and audio, save synchronized MP4. Optionally narrate via text-to-speech (`spkbak`).

### Desktop UI testing
Drive native applications via OCR (`txtfnd`) and image matching (`imgfnd`) instead of brittle pixel coordinates. Wait for windows/text to appear (`waitfr`) instead of fixed sleeps.

### Record & replay workflows
Use `stpcap` to record yourself doing a task, then replay it via `playbk` — no manual scripting required for simple workflows.

### Composition via shell pipes
Tools output machine-readable data on stdout, status on stderr — pipe them together:
```bash
# find text on screen and click it
inpctl --window "Notepad*" --leftmouse $(txtfnd --window "Notepad*" --text "File")
```

### Toolkit utility companion
`cnvrtr` handles encodings (base64, hex, hashing), units (length, mass, temperature, time, data, pressure, energy), date formats, number bases, and file conversions via ffmpeg — 200+ formats. Useful as a general-purpose dev utility.

## Documentation

- [Setup & Prerequisites](docs/setup.md) — .NET 10, ffmpeg, platform-specific requirements
- [Script Authoring Guide](docs/scripting.md) — writing `.idleops.yaml` automation scripts
- [Per-tool guides](docs/README.md#per-tool-guides) — detailed usage for each component

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [ffmpeg](https://ffmpeg.org/download.html) on PATH
- See [setup guide](docs/setup.md) for platform-specific details

## License

MIT
