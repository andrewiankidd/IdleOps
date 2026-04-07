# stpcap

Windows-only CLI that records keyboard and mouse input into an `.idleops.yaml` script. The inverse of playbk — perform actions once, then replay them.

- **Docs**: [docs/stpcap/](../../docs/stpcap/README.md)
- **Build**: `dotnet build src/stpcap/stpcap.csproj`
- **Test**: `dotnet test src/stpcap.Tests/stpcap.Tests.csproj`
- **Dependencies**: shared
- **Platforms**: Windows only (low-level keyboard/mouse hooks via SetWindowsHookEx)
