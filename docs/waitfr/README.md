# waitfr — Wait for Condition

> **Platform:** 🟢 Windows · 🟢 Linux (X11) · 🟢 macOS  —  🟢 works · 🟡 partial · 🔴 not available
>
> **Linux (X11):** window presence via `xdotool`; the optional `--text` OCR wait needs `tesseract-ocr`.
> **macOS:** window presence via `osascript`/System Events (needs **Accessibility**); the optional `--text` OCR wait needs `brew install tesseract` and **Screen Recording**. Verified on macOS 26.

Wait for a window to appear (or disappear), optionally waiting for specific text via OCR.

## Usage

```bash
# wait for a window to appear
dotnet run --project src/waitfr -- --window "My App*" --timeout 10

# wait for text to appear in a window
dotnet run --project src/waitfr -- --window "My App*" --text "Ready" --timeout 15

# wait for a window to disappear (e.g., loading dialog)
dotnet run --project src/waitfr -- --window "Loading*" --gone --timeout 30
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-w, --window` | Window title pattern | required |
| `-t, --text` | Text to wait for via OCR | none |
| `--timeout` | Seconds before giving up | 10 |
| `--gone` | Wait for window to disappear | false |

Exit code 0 = condition met, 1 = timeout.
