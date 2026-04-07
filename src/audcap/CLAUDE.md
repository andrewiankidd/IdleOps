# CLAUDE.md — audcap

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform system audio capture CLI. Records system audio output (loopback) to WAV files. Uses NAudio WASAPI loopback on Windows; spawns ffmpeg on macOS (avfoundation) and Linux (PulseAudio).

## Key Types

| Type | Purpose |
|------|---------|
| `IAudioCapturer` | Interface: `CaptureAsync(outputPath, token)` |
| `AudioCapturerFactory` | Returns platform-specific implementation |
| `WindowsAudioCapturer` | NAudio `WasapiLoopbackCapture` + `WaveFileWriter` |
| `FfmpegAudioCapturer` | Abstract base for macOS/Linux ffmpeg subprocess |
| `MacAudioCapturer` | avfoundation input `:0` |
| `LinuxAudioCapturer` | PulseAudio `-f pulse -i default` |
| `AudcapService` | Public async service wrapper for programmatic use |
| `Options` | Record: Output, Delay, Timer, ShowHelp, ShowVersion |
| `OptionsParser` | Manual CLI arg parsing |

## Platform Behavior

- **Windows**: No external dependencies. Uses NAudio WASAPI loopback device.
- **macOS**: Requires ffmpeg + loopback driver (BlackHole/Loopback).
- **Linux**: Requires ffmpeg + PulseAudio.

## Dependencies

- NuGet: NAudio 2.2.1
- Project: shared

## Build & Test

```powershell
dotnet build src/audcap/audcap.csproj
dotnet test src/audcap.Tests/audcap.Tests.csproj
```

Tests are Windows-only and require audio playback capability (they generate test tones via NAudio `SignalGenerator`).
