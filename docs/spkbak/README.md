# spkbak — Text-to-Speech

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Speak text aloud or save speech to a WAV file. Cross-platform: WinRT
`SpeechSynthesizer` on Windows, `say` on macOS, `espeak` on Linux (`apt install espeak`).

> On this multi-target project, `dotnet run` needs `-f <tfm>` (e.g.
> `-f net10.0-windows10.0.22621.0` on Windows). The built exe runs directly with no flag.

## Usage

```bash
# speak text through speakers
dotnet run --project src/spkbak -- --text "Welcome to the tutorial"

# save to WAV file
dotnet run --project src/spkbak -- --text "Click the settings button" --output narration.wav

# read from file
dotnet run --project src/spkbak -- --file script.txt --output narration.wav

# list available voices
dotnet run --project src/spkbak -- --list

# use a specific voice
dotnet run --project src/spkbak -- --text "Hello" --voice "Zira"
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-t, --text` | Text to speak | |
| `-f, --file` | Read text from file | |
| `-o, --output` | Save to WAV file (instead of playing) | play through speakers |
| `--voice` | Voice name (use `--list` to see options) | system default |
| `--list` | List available voices | |
