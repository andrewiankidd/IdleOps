# vidcap — Video Capture

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Record the screen or a specific window to MP4.

## Usage

```bash
# record full desktop for 10 seconds
dotnet run --project src/vidcap -- -t 10 -o desktop.mp4

# record a specific window by title (Windows only)
dotnet run --project src/vidcap -- -w "Notepad*" -t 10 -o notepad.mp4

# delay 3 seconds, then record for 5 seconds
dotnet run --project src/vidcap -- -d 3 -t 5 -o clip.mp4
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-o, --output` | Output MP4 file path | `cap.mp4` |
| `-d, --delay` | Seconds to wait before starting capture | none |
| `-t, --timer, --duration` | Maximum capture duration in seconds | none (until Ctrl+C) |
| `-w, --window` | Window title pattern for targeted capture (Windows only) | none (full desktop) |
| `-h, --help` | Show help | |
| `-v, --version` | Show version | |

## Window Matching (Windows)

The `--window` flag accepts wildcard patterns using `*`:

| Pattern | Matches |
|---------|---------|
| `Notepad*` | Any window starting with "Notepad" |
| `*Notepad` | Any window ending with "Notepad" |
| `*Note*` | Any window containing "Note" |
| `My*App` | "My" then anything then "App" |

Matching is case-insensitive. When multiple windows match, the most recently started process is selected.

On macOS and Linux, `--window` is accepted but ignored with a warning — full display capture is used instead.

## How It Works

All platforms use ffmpeg as the capture backend:
- **Windows**: gdigrab input. For window capture, P/Invoke finds the window position/size and captures that screen region.
- **macOS**: avfoundation full-display capture.
- **Linux**: x11grab using the `DISPLAY` environment variable.

Video is encoded with libx264, ultrafast preset, yuv420p pixel format.
