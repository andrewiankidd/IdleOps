# audcap — Audio Capture

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Record system audio (loopback) to a WAV file.

## Usage

```bash
# record until Ctrl+C
dotnet run --project src/audcap -- -o recording.wav

# record for 5 seconds after a 2-second delay
dotnet run --project src/audcap -- -d 2 -t 5 -o recording.wav
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-o, --output` | Output WAV file path | `cap.wav` |
| `-d, --delay` | Seconds to wait before starting capture | none |
| `-t, --timer, --duration` | Maximum capture duration in seconds | none (until Ctrl+C) |
| `-h, --help` | Show help | |
| `-v, --version` | Show version | |

## How It Works

- **Windows**: Uses NAudio's WASAPI loopback capture — records whatever audio is playing through the default output device. No external tools needed.
- **macOS**: Spawns ffmpeg with avfoundation input. Requires a loopback audio driver (BlackHole or Loopback).
- **Linux**: Spawns ffmpeg with PulseAudio input (`-f pulse -i default`).

## Tips

- If recording silence, make sure audio is actually playing through the default output device
- On Windows, the recording format matches the system's audio output format
- Press Ctrl+C for a clean stop — the WAV file will be properly finalized
