# cnvrtr

Cross-platform universal converter. Handles encodings, hashing, string transforms, number bases, date formats, unit conversions (16+ categories), and file format conversion via ffmpeg.

- **Docs**: [docs/cnvrtr/](../../docs/cnvrtr/README.md)
- **Build**: `dotnet build src/cnvrtr/cnvrtr.csproj`
- **Test**: `dotnet test src/cnvrtr.Tests/cnvrtr.Tests.csproj`
- **Dependencies**: none (file conversion requires ffmpeg on PATH)
- **Platforms**: cross-platform
- **Formats**: run `cnvrtr --list` for the full list (~200+ format aliases)
