# inpctl

Windows-only CLI for sending keyboard/mouse input to target windows and managing window state. Supports wildcard title matching, key chords, text typing, mouse clicks/drags, and window resize/move/maximize.

- **Docs**: [docs/inpctl/](../../docs/inpctl/README.md)
- **Build**: `dotnet build src/inpctl/inpctl.csproj`
- **Test**: `dotnet test src/inpctl.Tests/inpctl.Tests.csproj`
- **Dependencies**: shared
- **Platforms**: Windows only (P/Invoke to user32.dll)
