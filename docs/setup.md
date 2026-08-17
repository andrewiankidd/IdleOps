# Setup & Prerequisites

## .NET SDK

All IdleOps tools require the .NET 10.0 SDK.

- Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- Verify: `dotnet --version` should show 10.x

## ffmpeg

Required for video capture, audio capture on macOS/Linux, and audio/video merging.

### Windows
Download from [ffmpeg.org](https://ffmpeg.org/download.html) and add to PATH, or install via:
```powershell
winget install ffmpeg
# or
choco install ffmpeg
```

### macOS
```bash
brew install ffmpeg
```

### Linux
```bash
sudo apt install ffmpeg    # Debian/Ubuntu
sudo dnf install ffmpeg    # Fedora
```

Verify: `ffmpeg -version`

## Platform-Specific Requirements

### Windows
- No additional requirements for audio capture (uses NAudio WASAPI loopback)
- No additional requirements for video capture (uses gdigrab)
- inpctl works out of the box

### macOS

Verified on macOS 26 (Apple silicon). Install the CLIs the backends shell out to:

```bash
brew install ffmpeg cliclick tesseract
```

- **Audio capture** additionally requires a loopback driver — [BlackHole](https://github.com/ExistentialAudio/BlackHole) or [Loopback](https://rogueamoeba.com/loopback/) — because macOS exposes no system-audio input. audcap picks it by name; without one it records the microphone and warns.
- **Video capture** works via avfoundation; the screen device is resolved by name.
- **Input (inpctl), window control and uiactl** use `cliclick` and `osascript`/System Events.
- **OCR (txtfnd)** uses Tesseract.

**Privacy permissions.** macOS gates all of this behind TCC. Grant these to *the app that
runs the tools* — Terminal, iTerm, or your IDE — under System Settings › Privacy & Security:

| Permission | Needed by | Symptom if missing |
|---|---|---|
| **Accessibility** | inpctl, uiactl, window lookup (waitfr/scrcap/txtfnd) | Tools report the denial explicitly. `cliclick` itself exits 0 doing nothing, so inpctl detects this and fails instead. |
| **Screen Recording** | scrcap, vidcap, txtfnd, imgfnd, playbk screenshots | `screencapture` fails; ffmpeg does not list a screen device at all. |
| **Microphone** | audcap | ffmpeg blocks indefinitely on the consent prompt. |

Screen Recording changes need the host app restarted before they take effect.

**Retina displays.** Captures are written at native pixel resolution while window bounds and
synthetic input are in points (a 2× difference). `txtfnd` and `imgfnd` convert their output
to points, so piping them into `inpctl` is correct — but coordinates you read off a
screenshot by hand need halving.

### Linux
- **Audio capture** requires PulseAudio (`-f pulse -i default`)
- Video capture uses x11grab (requires X11, not Wayland)
- inpctl is not available (Windows-only)

## Building

```bash
# clone and build
git clone <repo-url>
cd IdleOps
dotnet build IdleOps.sln

# run tests (Windows recommended — some tests require audio/display hardware)
dotnet test IdleOps.sln
```

## Optional: ffprobe

Used by some tests to validate video output resolution. Typically installed alongside ffmpeg.

Verify: `ffprobe -version`
