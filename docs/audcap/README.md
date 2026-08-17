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
- **macOS**: Spawns ffmpeg with avfoundation input, selecting the loopback device **by name** (BlackHole/Loopback/Soundflower/Aggregate) — avfoundation indices are machine-specific, and index 0 is the built-in microphone on a stock Mac. With no loopback driver installed it falls back to the first input and warns that it is recording a microphone, not system audio. Set `IDLEOPS_AVFOUNDATION_AUDIO=<index>` to choose explicitly. Needs **Microphone** permission; without it ffmpeg blocks on the consent prompt. Note the avfoundation device takes ~1.5–2.5s to open and that time counts against `--timer`, so ask for a little more than you need.
- **Linux**: Spawns ffmpeg with PulseAudio input (`-f pulse -i default`).

## Tips

- If recording silence, make sure audio is actually playing through the default output device
- On Windows, the recording format matches the system's audio output format
- Press Ctrl+C for a clean stop — the WAV file will be properly finalized
