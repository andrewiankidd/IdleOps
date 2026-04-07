# External Tools

## ffmpeg

Required on PATH. Used by vidcap, audcap (macOS/Linux), and outcap (merge).

### Video Capture (vidcap)

**Windows — full desktop:**
```
ffmpeg -f gdigrab -framerate 30 -i desktop -c:v libx264 -preset ultrafast -pix_fmt yuv420p output.mp4
```

**Windows — window region:**
```
ffmpeg -f gdigrab -framerate 30 -offset_x {x} -offset_y {y} -video_size {w}x{h} -i desktop -c:v libx264 -preset ultrafast -pix_fmt yuv420p output.mp4
```

**macOS:**
```
ffmpeg -f avfoundation -framerate 30 -i "1" -c:v libx264 -preset ultrafast -pix_fmt yuv420p output.mp4
```

**Linux:**
```
ffmpeg -f x11grab -framerate 30 -i :0.0 -c:v libx264 -preset ultrafast -pix_fmt yuv420p output.mp4
```

### Audio Capture (audcap — non-Windows)

**macOS:**
```
ffmpeg -f avfoundation -i ":0" output.wav
```

**Linux:**
```
ffmpeg -f pulse -i default output.wav
```

### A/V Merge (outcap)

```
ffmpeg -itsoffset {offset_seconds} -i video.mp4 -i audio.wav -c:v copy -c:a aac -shortest merged.mp4
```

The `-itsoffset` value is calculated from the difference in `StartTimeUtc` between the audio and video captures to synchronize them.

### Graceful Shutdown

All ffmpeg subprocesses are stopped by writing `q` to stdin. If the process doesn't exit within 3 seconds, it is killed.

## ffprobe

Used in tests to validate capture output.

```
ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 video.mp4
```

Returns `{width}x{height}` (e.g., `1920x1080`).

## NAudio (NuGet, not external)

Used by audcap on Windows only. Key classes:
- `WasapiLoopbackCapture` — captures system audio output
- `WaveFileWriter` — writes captured audio to WAV format
- `SignalGenerator` — generates test tones (used in tests)

## Windows APIs (P/Invoke)

### Window Enumeration (vidcap, inpctl)
- `EnumWindows` — iterate all top-level windows
- `GetWindowText` — get window title
- `GetWindowRect` — get window position/size (RECT)
- `IsWindowVisible` — filter hidden windows

### Input Simulation (inpctl)
- `PostMessage` — send keyboard messages (WM_KEYDOWN/WM_KEYUP/WM_CHAR)
- `SendInput` — inject mouse events (MOUSEEVENTF_ABSOLUTE, MOUSEEVENTF_LEFTDOWN, etc.)
- `VkKeyScan` — map characters to virtual key codes (handles shift state)
- `SetCursorPos` — physically move mouse cursor

### Process Control (inpctl)
- `AttachConsole` — attach to target process console
- `GenerateConsoleCtrlEvent` — send CTRL+C signal
- `FreeConsole` — detach from console
