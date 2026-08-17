# playbk — Script Execution Engine

> **Platform:** 🟢 Windows · 🟢 Linux (X11) · 🟢 macOS  —  🟢 works · 🟡 partial · 🔴 not available
>
> **Linux (X11):** the orchestrator runs; OCR (`click-text`/`assert-text`) uses Tesseract, capture uses ImageMagick, window/input use xdotool — so it needs `tesseract-ocr`, `imagemagick`, `xdotool` plus the sibling IdleOps tools on PATH. `exec`-launched apps and Wayland caveats follow the individual tools above.
> **macOS:** verified end-to-end on macOS 26 (`wait-window`, `keyboard`, `type`, `screenshot`, `assert-text`). Needs the per-tool macOS prerequisites above — `brew install cliclick tesseract ffmpeg` plus **Accessibility** and **Screen Recording** permission for whichever app launches playbk. Inherits each tool's macOS limits: no `stpcap` recorder, and no `--element-at` for the UIA verbs.

Execute YAML automation scripts that drive desktop workflows — launching apps, sending input, and capturing media.

## Usage

```bash
# run a specific script
dotnet run --project src/playbk -- -i src/playbk/inputs/rickroll.idleops.yaml -o ./outputs

# run all scripts matching a pattern
dotnet run --project src/playbk -- -i "scripts/*.yaml" -o ./captures

# use default input patterns (inputs/*.idleops.yaml, inputs/*.yaml)
dotnet run --project src/playbk
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-i, --input` | Input script pattern(s), comma-separated | `inputs/*.idleops.yaml, inputs/*.yaml` |
| `-o, --output` | Output directory for captures | `outputs` |
| `-h, --help` | Show help | |
| `-v, --version` | Show version | |

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PLAYBK_CAPTURE_TIMER` | Default capture duration in seconds | `10` |

## How It Works

1. Resolves input YAML files using glob patterns
2. Adds its own output directory to PATH (contains outcap and inpctl binaries copied at build time)
3. For each script, loads the YAML and executes steps sequentially
4. Each `exec` step launches a process — either waiting for completion or firing and forgetting
5. Process IDs are tracked for `%id_pid%` token expansion

See the [Script Authoring Guide](../scripting.md) for full details on writing scripts.

## Included Example Scripts

| Script | What it does |
|--------|-------------|
| `rickroll.idleops.yaml` | Opens Rick Astley on YouTube, records 10s of audio+video |
| `notepad-hello-world.idleops.yaml` | Launches Notepad, types text via inpctl, records result |
| `mspaint-smiley.idleops.yaml` | Launches Paint, draws a smiley face with percentage-based mouse input |
