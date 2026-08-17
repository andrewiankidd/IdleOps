# CLAUDE.md — txtfnd

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform CLI that finds on-screen text within a window and outputs its center
coordinates as `x,y` (for piping into inpctl). Multi-targeted: the Windows TFM uses
WinRT OCR (zero-install, warm engine); the net10.0 TFM uses Tesseract, so txtfnd runs
on Linux/macOS too.

The pipeline is unified in `shared`: capture the window with `IScreenCapturer`,
recognize words with an `ITextRecognizer`, locate the target with `TextLocator`.
Only the recognizer differs per platform. (playbk still uses the in-process
`WindowTextFinder` with its warm engine for repeated OCR across steps.)

## Key Types

| Type | Where | Purpose |
|------|-------|---------|
| `ITextRecognizer` / `RecognizedWord` | shared | Image path → words with bounding boxes |
| `TesseractTextRecognizer` | shared | `tesseract img stdout --psm 11 tsv` → parse TSV (Linux/macOS) |
| `WinRtTextRecognizer` | shared.win | WinRT `OcrEngine` (Windows), reuses `OcrService` upscale trick |
| `TextLocator` | shared | Locate target in words: single word, then shortest consecutive span |
| `ImageTextFinder` | shared | Capture → recognize → locate; injects capturer + recognizer |
| `WindowTextFinder` | shared.win | In-process capture+OCR with a warm engine (used by playbk, not txtfnd) |
| `Program` | txtfnd | `#if WINDOWS` picks WinRT else Tesseract → `ImageTextFinder` → print `x,y` |

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
- Project: shared (always), shared.win (Windows TFM only, conditional ProjectReference)
- Platform: **Windows** 🟢 WinRT OCR (needs an OCR language pack) · **Linux** 🟢 Tesseract (`apt install tesseract-ocr`), verified under Xvfb in `scripts/linux-e2e.sh` · **macOS** 🟡 Tesseract (`brew install tesseract`) but capture is full-screen only (see scrcap)

## Build

```bash
# Windows (both TFMs) / non-Windows (net10.0 TFM only):
dotnet build src/txtfnd/txtfnd.csproj                 # Windows
dotnet build src/txtfnd/txtfnd.csproj -f net10.0 -p:EnableWindowsTargeting=true   # Linux/macOS
```

The `EnableWindowsTargeting` flag lets the net10.0 build resolve the reference graph
(which still lists shared.win for the Windows TFM) without building shared.win.
