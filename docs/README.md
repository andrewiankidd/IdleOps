# IdleOps Documentation

IdleOps automates desktop workflows — launching apps, sending keyboard/mouse input, capturing audio and video, finding UI elements via OCR — all driven by YAML scripts. It's designed for use cases like recording video documentation, automating screenshot generation, and creating reproducible desktop automation flows.

Unlike browser-only tools like Cypress or Playwright, IdleOps works across the entire desktop: native applications, system audio, window management, and more.

## Getting Started

- [Setup & Prerequisites](setup.md) — what you need installed before using IdleOps
- [Script Authoring Guide](scripting.md) — how to write `.idleops.yaml` scripts for playbk

## Per-Tool Guides

### Capture

| Tool | Guide | What it does |
|------|-------|-------------|
| audcap | [docs/audcap/](audcap/README.md) | Record system audio to WAV |
| vidcap | [docs/vidcap/](vidcap/README.md) | Record screen or window to MP4 |
| outcap | [docs/outcap/](outcap/README.md) | Capture audio + video together, merge into one file |
| scrcap | [docs/scrcap/](scrcap/README.md) | Screenshot a window to PNG/JPEG |

### Automation

| Tool | Guide | What it does |
|------|-------|-------------|
| playbk | [docs/playbk/](playbk/README.md) | Execute YAML automation scripts |
| inpctl | [docs/inpctl/](inpctl/README.md) | Send keyboard/mouse input, manage windows |
| txtfnd | [docs/txtfnd/](txtfnd/README.md) | Find text on screen via OCR, return coordinates |
| imgfnd | [docs/imgfnd/](imgfnd/README.md) | Find UI elements by reference image |
| waitfr | [docs/waitfr/](waitfr/README.md) | Wait for a window or text to appear |
| stpcap | [docs/stpcap/](stpcap/README.md) | Record user input into YAML scripts |

### Utilities

| Tool | Guide | What it does |
|------|-------|-------------|
| spkbak | [docs/spkbak/](spkbak/README.md) | Text-to-speech (speak or save to WAV) |
| cnvrtr | [docs/cnvrtr/](cnvrtr/README.md) | Universal converter (encodings, units, dates, files) |

## Quick Start

```bash
# build everything
dotnet build IdleOps.sln

# run the rickroll demo
dotnet run --project src/playbk -- -i src/playbk/inputs/rickroll.idleops.yaml -o ./outputs

# find text on screen and click it
dotnet run --project src/txtfnd -- -w "Notepad*" -t "File"

# screenshot a window
dotnet run --project src/scrcap -- -w "Notepad*" -o screenshot.png

# convert units
dotnet run --project src/cnvrtr -- --value "100" --from celsius --to fahrenheit
```
