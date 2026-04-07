# audcap

Cross-platform system audio capture CLI. Records system audio output (loopback) to WAV.

- **Docs**: [docs/audcap/](../../docs/audcap/README.md)
- **Build**: `dotnet build src/audcap/audcap.csproj`
- **Test**: `dotnet test src/audcap.Tests/audcap.Tests.csproj`
- **Dependencies**: NAudio 2.2.1, shared
- **Platforms**: Windows (WASAPI), macOS (ffmpeg+avfoundation), Linux (ffmpeg+PulseAudio)
