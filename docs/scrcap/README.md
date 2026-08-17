# scrcap — Screenshot Capture

> **Platform:** 🟢 Windows · 🟢 Linux (X11) · 🟡 macOS  —  🟢 works · 🟡 partial · 🔴 not available
>
> **Linux (X11):** needs ImageMagick (`import`) on PATH; window id resolved via `xdotool`. Use `--window screen` for the whole display. Wayland windows are reachable only via XWayland.
> **macOS:** `screencapture` — whole-display via `-x`; per-window now captures the window's region (`-R`) using bounds from `osascript`/System Events. UNVERIFIED (written without a Mac); needs Accessibility permission for the bounds lookup.

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
