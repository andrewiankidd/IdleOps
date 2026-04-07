# spkbak — Text-to-Speech

Speak text aloud or save speech to a WAV file using the Windows TTS engine.

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
