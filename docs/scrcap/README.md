# scrcap — Screenshot Capture

> **Platform:** 🟢 Windows · 🟢 Linux (X11) · 🟢 macOS  —  🟢 works · 🟡 partial · 🔴 not available
>
> **Linux (X11):** needs ImageMagick (`import`) on PATH; window id resolved via `xdotool`. Use `--window screen` for the whole display. Wayland windows are reachable only via XWayland.
> **macOS:** `screencapture` — whole-display via `-x`, per-window via `-R` using bounds from `osascript`/System Events. Verified on macOS 26. Needs **Screen Recording** permission (and **Accessibility** for the bounds lookup); without them it now fails with a message naming the missing permission. **Retina:** the saved file is native pixels while `-R` is points, so a 656×422-point window is written as a 1312×844 image — the reported `Scale` carries the factor, and `txtfnd`/`imgfnd` already apply it.

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
