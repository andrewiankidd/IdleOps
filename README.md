# IdleOps

IdleOps is a modular toolkit for automating desktop interactions, capturing media, and replaying scripted workflows. Think Cypress/Playwright-style config-driven steps, but for the whole desktop — native apps, system audio, screen recording, keyboard/mouse simulation, and more.

## Components

Platform support: 🟢 works · 🟡 partial · 🔴 not available.

> **macOS status:** the macOS backends have now been **run and verified on real hardware**
> (macOS 26, Apple silicon, Retina): screenshots, OCR, image matching, mouse/keyboard input,
> window management, screen recording, loopback audio, text-to-speech and end-to-end `playbk`
> runs all work. Two gaps remain: `stpcap` has no recorder backend, and `uiactl` cannot do
> `--element-at`. Before first use:
>
> 1. `brew install cliclick tesseract ffmpeg` (plus [BlackHole](https://github.com/ExistentialAudio/BlackHole) for `audcap` — macOS exposes no system-audio input of its own).
> 2. Grant the app that runs the tools (Terminal, iTerm, or your IDE) **Accessibility** and
>    **Screen Recording** under System Settings › Privacy & Security — plus **Microphone**
>    for `audcap`. Without them the tools now fail with an explicit message rather than
>    silently doing nothing.
>
> **Retina note:** captures are saved at native pixel resolution while window bounds and
> input are in points. `txtfnd`/`imgfnd` return point coordinates, so their output feeds
> straight into `inpctl` — but a raw pixel coordinate read off a screenshot yourself will be
> 2× too large.

### Capture
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[audcap](docs/audcap/README.md)** | Cross-platform system audio capture (WAV) | 🟢 | 🟢 | 🟢 |
| **[vidcap](docs/vidcap/README.md)** | Cross-platform screen/window video capture (MP4) | 🟢 | 🟢 | 🟢 |
| **[outcap](docs/outcap/README.md)** | Synchronized audio + video capture and merge | 🟢 | 🟢 | 🟢 |
| **[scrcap](docs/scrcap/README.md)** | Window screenshot capture (PNG/JPEG/BMP) | 🟢 | 🟢 | 🟢 |

### Automation
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[playbk](docs/playbk/README.md)** | YAML script execution engine (exec, sleep, wait-window, click-text, assert-text, type, keyboard, screenshot, speak, UIA verbs) | 🟢 | 🟢 | 🟢 |
| **[inpctl](docs/inpctl/README.md)** | Keyboard/mouse input + window management | 🟢 | 🟢 | 🟢 |
| **[uiactl](docs/uiactl/README.md)** | Element automation by accessibility tree (Windows UIA / Linux AT-SPI2), focus-free | 🟢 | 🟢 | 🟡 |
| **[txtfnd](docs/txtfnd/README.md)** | Find text on screen via OCR, return coordinates | 🟢 | 🟢 | 🟢 |
| **[imgfnd](docs/imgfnd/README.md)** | Find UI elements by reference image (pure-managed template match) | 🟢 | 🟢 | 🟢 |
| **[waitfr](docs/waitfr/README.md)** | Wait for window/text conditions | 🟢 | 🟢 | 🟢 |
| **[stpcap](docs/stpcap/README.md)** | Record user input into YAML scripts (Windows hooks / Linux XRecord) | 🟢 | 🟢 | 🟡 |

### Utilities
| Tool | Description | Win | Lin | Mac |
|------|-------------|:---:|:---:|:---:|
| **[spkbak](docs/spkbak/README.md)** | Text-to-speech — WinRT on Windows, `say`/`espeak` on macOS/Linux | 🟢 | 🟢 | 🟢 |
| **[cnvrtr](docs/cnvrtr/README.md)** | Universal converter — encodings, units, dates, files (200+ formats) | 🟢 | 🟢 | 🟢 |

## Download

Prebuilt bundles are attached to the [`latest-main`](https://github.com/andrewiankidd/IdleOps/releases/tag/latest-main) release, refreshed on every green push to `main`:

| Platform | Asset | Contents |
|---|---|---|
| Windows | [`idleops-win-x64.zip`](https://github.com/andrewiankidd/IdleOps/releases/download/latest-main/idleops-win-x64.zip) | every tool |
| Linux (X11) | [`idleops-linux-x64.tar.gz`](https://github.com/andrewiankidd/IdleOps/releases/download/latest-main/idleops-linux-x64.tar.gz) | the cross-platform tools |
| macOS (Apple silicon) | [`idleops-osx-arm64.tar.gz`](https://github.com/andrewiankidd/IdleOps/releases/download/latest-main/idleops-osx-arm64.tar.gz) | the cross-platform tools |

They are framework-dependent, so they need the [.NET 10 runtime](https://dotnet.microsoft.com/download) — plus the per-platform prerequisites in [setup](docs/setup.md). Or build from source:

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
> - **[POSEIDON](https://github.com/andrewiankidd/POSEIDON)** — doc screenshots for a Rust/Tauri app, shot against the **web** build in a browser (the same bundle the Tauri window wraps) on a local playground instance seeded with stub demo data. Script lives in the POSEIDON repo at [`tools/screenshots/`](https://github.com/andrewiankidd/POSEIDON/tree/main/tools/screenshots) (`poseidon-web-screenshots.idleops.yaml`).
>
> Both keep their `.idleops.yaml` scripts in their own repos and link back to this project's [schema](schema/idleops.schema.json) via a `# yaml-language-server: $schema=…` modeline.

### Automated video documentation / demo recording
Open an app, perform a workflow (type, click, navigate menus), record both screen and audio, save synchronized MP4. Optionally narrate via text-to-speech (`spkbak`).

### Desktop UI testing
Drive native applications via OCR (`txtfnd`) and image matching (`imgfnd`) instead of brittle pixel coordinates. Wait for windows/text to appear (`waitfr`) instead of fixed sleeps.

### Record & replay workflows
Use `stpcap` to record yourself doing a task, then replay it via `playbk` — no manual scripting required for simple workflows.

### Sustained / background input
Hold a key down for a duration with `inpctl --hold`. `--method background` posts to a window without stealing focus — e.g. holding "F" in **Palworld** (an Unreal game) to keep an action running while you work in another window, which the game's own toggle stops doing on focus loss:
```bash
inpctl --window "Palworld*" --hold "F" --method background --duration 3600
```
Only works on targets that process their window message queue (Unreal games do; RawInput/DirectInput-only games need `--method foreground`, which requires focus). Input automation on official multiplayer is anti-cheat territory — keep it to singleplayer / private servers.

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
