# CLAUDE.md — vidcap

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform screen/window video capture CLI. Records desktop or a specific window to MP4 via ffmpeg. On Windows, supports targeting a window by title pattern using P/Invoke for window enumeration and gdigrab for region capture.

## Key Types

| Type | Purpose |
|------|---------|
| `IVideoCapturer` | Interface: `CaptureAsync(outputPath, windowTitle, token)` |
| `VideoCapturerFactory` | Returns platform-specific implementation |
| `WindowsVideoCapturer` | P/Invoke `EnumWindows`/`GetWindowRect` + gdigrab via ffmpeg. Builds regex from wildcard patterns. |
| `FfmpegVideoCapturer` | Abstract base for platform ffmpeg invocation. Graceful stop via stdin `q` with 3s timeout. |
| `MacVideoCapturer` | avfoundation full-display capture (ignores `--window` with warning) |
| `LinuxVideoCapturer` | x11grab using `DISPLAY` env var (ignores `--window` with warning) |
| `VidcapService` | Public async service wrapper |
| `Options` | Record: Output, Delay, Timer, WindowTitle, ShowHelp, ShowVersion |

## Platform Behavior

- **Windows**: gdigrab with window matching via wildcard patterns (`*` anywhere). Uses `EnumWindows`, `GetWindowRect`, `IsWindowVisible`, `GetWindowText` from user32.dll.
- **macOS**: avfoundation full display. `--window` ignored with warning.
- **Linux**: x11grab. `--window` ignored with warning.

## Dependencies

- NuGet: none
- Project: shared
- External: ffmpeg on PATH

## Build & Test

```powershell
dotnet build src/vidcap/vidcap.csproj
dotnet test src/vidcap.Tests/vidcap.Tests.csproj
```

Tests are Windows-only. They launch Notepad, capture desktop vs window, and use ffprobe to validate resolution.
