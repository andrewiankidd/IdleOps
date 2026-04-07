# CLAUDE.md — txtfnd

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Windows-only CLI tool that screenshots a window, runs OCR via `Windows.Media.Ocr`, finds a target text string, and outputs its center coordinates as `x,y`. Designed to be piped into inpctl for "find text and click it" automation.

## Key Types

| Type | Purpose |
|------|---------|
| `OcrService` | Converts Bitmap → SoftwareBitmap, runs OCR, searches for text matches across word spans |
| `OcrTextResult` | Record: Text, X, Y, Width, Height for a recognized word |
| `Options` | Record: Window, Text, ShowHelp, ShowVersion |
| `OptionsParser` | Manual CLI arg parsing |

## OCR Pipeline

1. `WindowCapture.CaptureWindow(handle)` → `System.Drawing.Bitmap` (from shared)
2. Bitmap saved to PNG `MemoryStream` → `BitmapDecoder` → `SoftwareBitmap`
3. `OcrEngine.TryCreateFromUserProfileLanguages()` → `RecognizeAsync(softwareBitmap)`
4. Search `OcrResult.Lines[].Words[]` for target text (single word match, then multi-word span)
5. Return center of matched word bounding rect

## Dependencies

- NuGet: none (WinRT APIs come from the `-windows10.0.22621.0` TFM)
- Project: shared (for WindowMatcher, WindowCapture, ConsoleLogger, HelpPrinter)
- Platform: Windows 10/11 with OCR language pack

## Build

```powershell
dotnet build src/txtfnd/txtfnd.csproj
```
