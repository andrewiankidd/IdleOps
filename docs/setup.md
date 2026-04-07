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
- **Audio capture** requires a loopback driver: [BlackHole](https://github.com/ExistentialAudio/BlackHole) or [Loopback](https://rogueamoeba.com/loopback/)
- Video capture works via avfoundation
- inpctl is not available (Windows-only)

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
