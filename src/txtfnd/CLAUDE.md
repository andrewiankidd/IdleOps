# CLAUDE.md — txtfnd

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Windows-only CLI that finds on-screen text within a window and outputs its center
coordinates as `x,y` (for piping into inpctl). A **thin shell** — all OCR logic
lives in `IdleOps.Shared.Win.WindowTextFinder` (project `shared.win`), so playbk
consumes the *same* code in-process (with a warm engine) instead of shelling out.

## Key Types

| Type | Where | Purpose |
|------|-------|---------|
| `WindowTextFinder` | shared.win | Capture window + OCR search; holds a warm `OcrEngine` reused across calls |
| `OcrService` / `OcrTextResult` | shared.win (internal/public) | Bitmap → SoftwareBitmap, word-span text search |
| `Options` / `OptionsParser` / `Program` | txtfnd | Parse args → `WindowTextFinder.FindAsync` → print `x,y` |

## OCR Pipeline (in `shared.win`)

1. `WindowCapture.CaptureWindow(handle)` → `System.Drawing.Bitmap` (from shared)
2. **Direct pixel copy** Bitmap(32bppArgb) → `SoftwareBitmap`(Bgra8) via `LockBits`
   + `CopyFromBuffer` — bypasses the WIC PNG encode/decode round-trip (which failed
   with `WINCODEC_ERR_COMPONENTNOTFOUND` and cost an encode+decode per frame)
3. `OcrEngine.RecognizeAsync(softwareBitmap)` (engine created once, reused)
4. Search `OcrResult.Lines[].Words[]` for target text (single word, then multi-word span)
5. Return center of matched word bounding rect

## Dependencies

- NuGet: none (WinRT APIs come from the `-windows10.0.22621.0` TFM)
- Project: shared, shared.win
- Platform: Windows 10/11 with OCR language pack

## Build

```powershell
dotnet build src/txtfnd/txtfnd.csproj
```
