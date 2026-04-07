# IdleOps

IdleOps is a modular toolkit for automating desktop interactions, capturing media, and replaying scripted workflows. Think Cypress/Playwright-style config-driven steps, but for the whole desktop — native apps, system audio, screen recording, keyboard/mouse simulation, and more.

## Components

| Tool | Description |
|------|-------------|
| **[audcap](docs/audcap/README.md)** | Cross-platform system audio capture CLI |
| **[vidcap](docs/vidcap/README.md)** | Cross-platform screen/window video capture CLI |
| **[outcap](docs/outcap/README.md)** | Audio + video capture with synchronized merge |
| **[playbk](docs/playbk/README.md)** | YAML script engine for repeatable automation flows |
| **[inpctl](docs/inpctl/README.md)** | Keyboard/mouse input to windows via wildcard matching (Windows) |
| **[txtfnd](docs/txtfnd/README.md)** | Find text on screen via OCR, return coordinates for clicking (Windows) |
| **[scrcap](src/scrcap/README.md)** | Capture a window screenshot to PNG/JPEG/BMP (Windows) |
| **stpcap** | Record user input into structured scripts (planned) |

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
