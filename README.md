# IdleOps

IdleOps is a modular toolkit for automating desktop interactions, capturing media, and replaying scripted workflows. Think Cypress/Playwright-style config-driven steps, but for the whole desktop — native apps, system audio, screen recording, keyboard/mouse simulation, and more.

## Components

### Capture
| Tool | Description |
|------|-------------|
| **[audcap](docs/audcap/README.md)** | Cross-platform system audio capture (WAV) |
| **[vidcap](docs/vidcap/README.md)** | Cross-platform screen/window video capture (MP4) |
| **[outcap](docs/outcap/README.md)** | Synchronized audio + video capture and merge |
| **[scrcap](docs/scrcap/README.md)** | Window screenshot capture (PNG/JPEG/BMP) |

### Automation
| Tool | Description |
|------|-------------|
| **[playbk](docs/playbk/README.md)** | YAML script execution engine (exec, sleep, wait-window, click-text, screenshot) |
| **[inpctl](docs/inpctl/README.md)** | Keyboard/mouse input + window management (Windows) |
| **[txtfnd](docs/txtfnd/README.md)** | Find text on screen via OCR, return coordinates (Windows) |
| **[imgfnd](docs/imgfnd/README.md)** | Find UI elements by reference image (Windows) |
| **[waitfr](docs/waitfr/README.md)** | Wait for window/text conditions (Windows) |
| **[stpcap](docs/stpcap/README.md)** | Record user input into YAML scripts (Windows) |

### Utilities
| Tool | Description |
|------|-------------|
| **[spkbak](docs/spkbak/README.md)** | Text-to-speech (Windows) |
| **[cnvrtr](docs/cnvrtr/README.md)** | Universal converter — encodings, units, dates, files (200+ formats) |

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

> **Real-world example**: IdleOps was used to generate **15 screenshots** for the [Crosspose](https://github.com/andrewiankidd/Crosspose) repo — full sidebar navigation, container details tabs, dark/light mode variants, and per-GUI screens. See `src/playbk/inputs/crosspose-gui-screenshots.idleops.yaml` for the full script.

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
