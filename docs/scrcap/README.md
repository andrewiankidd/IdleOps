# scrcap — Screenshot Capture

> **Platform:** 🟢 Windows · 🟡 Linux · 🟡 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Capture a window screenshot and save to file.

## Usage

```bash
dotnet run --project src/scrcap -- --window "Notepad*" -o screenshot.png
dotnet run --project src/scrcap -- -w "*Chrome*" -o browser.jpg
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-w, --window` | Window title pattern (supports `*` wildcards) | required |
| `-o, --output` | Output file path | `screenshot.png` |
| `-h, --help` | Show help | |

Supports PNG (default), JPEG, BMP, GIF, TIFF — detected from file extension.

Uses `PrintWindow` which captures even partially occluded windows.
