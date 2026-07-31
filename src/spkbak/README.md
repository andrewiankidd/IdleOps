# spkbak

Cross-platform CLI for text-to-speech. Speaks text aloud through speakers or saves to a WAV file.

- **Backends**: WinRT `SpeechSynthesizer` on Windows; `say` on macOS; `espeak` on Linux
- **Docs**: [docs/spkbak/](../../docs/spkbak/README.md)
- **Build**: `dotnet build src/spkbak/spkbak.csproj`
- **Run**: the built exe runs directly. With `dotnet run` on this multi-target, pass `-f`
  (e.g. `dotnet run --project src/spkbak -f net10.0-windows10.0.22621.0 -- --text "hi"`)
- **Dependencies**: `System.Windows.Extensions` (Windows build only)
- **TFMs**: `net10.0` (macOS/Linux, shells out) and `net10.0-windows10.0.22621.0` (WinRT)
- **Platforms**: Windows 10/11, macOS (built-in `say`), Linux (`espeak` — `apt install espeak`)
