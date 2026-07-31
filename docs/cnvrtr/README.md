# cnvrtr — Universal Converter

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Convert between encodings, units, date formats, number bases, and file formats.

## Usage

```bash
# encoding
dotnet run --project src/cnvrtr -- --value "hello" --to base64
dotnet run --project src/cnvrtr -- --value "aGVsbG8=" --from base64

# hashing
dotnet run --project src/cnvrtr -- --value "hello" --to sha256

# units
dotnet run --project src/cnvrtr -- --value "100" --from celsius --to fahrenheit
dotnet run --project src/cnvrtr -- --value "5.5" --from miles --to km
dotnet run --project src/cnvrtr -- --value "1024" --from bytes --to mb

# dates
dotnet run --project src/cnvrtr -- --value "2026-04-06" --to unix

# number bases
dotnet run --project src/cnvrtr -- --value "255" --from dec --to hex

# string transforms
dotnet run --project src/cnvrtr -- --value "Hello World" --to slug

# file conversion (via ffmpeg)
dotnet run --project src/cnvrtr -- --value video.mp4 --to gif

# pipe from stdin
echo "hello" | dotnet run --project src/cnvrtr -- --to base64

# list all formats
dotnet run --project src/cnvrtr -- --list
```

## Options

| Flag | Description |
|------|-------------|
| `--value` | Input value (or pipe via stdin) |
| `--from` | Source format (often auto-detected) |
| `--to` | Target format (required) |
| `--list` | List all supported formats |

## Supported Categories

Encoding, hashing, string transforms, number bases, date/time, time units, length, mass, temperature, data storage, speed, area, volume, pressure, energy, power, frequency, angle, fuel economy, file formats (via ffmpeg).

Run `cnvrtr --list` for the full list.
