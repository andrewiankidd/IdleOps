# vidcap

Cross-platform screen/window video capture CLI. Records desktop or a specific window to MP4 via ffmpeg.

- **Docs**: [docs/vidcap/](../../docs/vidcap/README.md)
- **Build**: `dotnet build src/vidcap/vidcap.csproj`
- **Test**: `dotnet test src/vidcap.Tests/vidcap.Tests.csproj`
- **Dependencies**: shared
- **External**: ffmpeg on PATH
- **Platforms**: Windows (gdigrab + window matching), macOS (avfoundation), Linux (x11grab)
