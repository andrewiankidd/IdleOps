# playbk

YAML script execution engine. Runs `.idleops.yaml` scripts that define sequential automation steps — launching apps, sending input, capturing media, finding text via OCR.

- **Docs**: [docs/playbk/](../../docs/playbk/README.md)
- **Script Guide**: [docs/scripting.md](../../docs/scripting.md)
- **Build**: `dotnet build src/playbk/playbk.csproj`
- **Test**: `dotnet test src/playbk.Tests/playbk.Tests.csproj`
- **Dependencies**: shared, outcap, inpctl, YamlDotNet, FileSystemGlobbing
- **Built-in actions**: `exec`, `sleep`, `wait-window`, `click-text`, `screenshot`
- **Build wiring**: MSBuild targets copy outcap, inpctl, txtfnd, scrcap into output dir
