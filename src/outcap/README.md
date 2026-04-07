# outcap

Orchestrates simultaneous audio and video capture, then merges into a single synchronized MP4 with ffmpeg.

- **Docs**: [docs/outcap/](../../docs/outcap/README.md)
- **Build**: `dotnet build src/outcap/outcap.csproj`
- **Test**: `dotnet test src/outcap.Tests/outcap.Tests.csproj`
- **Dependencies**: shared, audcap, vidcap, YamlDotNet, FileSystemGlobbing
- **External**: ffmpeg on PATH
