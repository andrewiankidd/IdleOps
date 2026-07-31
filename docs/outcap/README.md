# outcap — Combined Audio/Video Capture

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Capture audio and video simultaneously, then merge into a single synchronized MP4.

## Usage

```bash
# basic 10-second capture
dotnet run --project src/outcap -- -t 10

# target a specific window with custom output
dotnet run --project src/outcap -- -w "My App*" -t 15 -o demo.mp4

# keep raw audio/video files after merge
dotnet run --project src/outcap -- -t 10 --keep-raw

# override audio and video settings independently
dotnet run --project src/outcap -- --vid-timer 10 --aud-timer 12 --vid-output video.mp4 --aud-output audio.wav -o merged.mp4
```

## Options

### Global Options (apply to both streams unless overridden)

| Flag | Description | Default |
|------|-------------|---------|
| `-o, --output` | Merged output MP4 path | `outcap.mp4` |
| `-t, --timer, --duration` | Capture duration for both streams | none |
| `-d, --delay` | Start delay for both streams | none |
| `-w, --window` | Window title for video capture | none |
| `--keep-raw` | Keep intermediate audio/video files | false (deleted after merge) |
| `-h, --help` | Show help | |
| `-v, --version` | Show version | |

### Per-Stream Overrides

| Flag | Description | Default |
|------|-------------|---------|
| `--vid-output` | Video output path | `outcap-video.mp4` |
| `--vid-delay` | Video start delay | global delay |
| `--vid-timer` | Video duration | global timer |
| `--aud-output` | Audio output path | `outcap-audio.wav` |
| `--aud-delay` | Audio start delay | global delay |
| `--aud-timer` | Audio duration | global timer |

## How It Works

1. Audio (audcap) and video (vidcap) capture start in parallel
2. Each capture records its `StartTimeUtc`
3. After both finish, the time offset between them is calculated
4. ffmpeg merges the streams using `-itsoffset` for synchronization
5. Raw files are deleted unless `--keep-raw` is set

If the merge fails, outcap falls back to copying the video file as the output.
