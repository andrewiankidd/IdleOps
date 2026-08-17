# imgfnd — Image Template Matching

> **Platform:** 🟢 Windows · 🟢 Linux · 🟢 macOS  —  🟢 works · 🟡 partial · 🔴 not available

Find a UI element by matching a reference image against a window screenshot. Returns center coordinates.

Matching is **pure managed** (ImageSharp for decoding + a SIMD normalized-cross-correlation, equivalent to OpenCV's `TM_CCOEFF_NORMED`) — no native OpenCV, so it runs identically on Windows, Linux and macOS. Capture uses the shared cross-platform capturer, including per-window capture on macOS.

On macOS the match is computed against the native-resolution Retina image but the printed centre is converted to points, so it feeds straight into `inpctl`. Reference images must come from a capture at the same scale — one cropped from a `scrcap` PNG on the same display is correct by construction. Note that a template covering a flat, single-colour region has no variance to correlate against and will score 0; crop something with detail in it.

## Usage

```bash
# find a button by reference image
dotnet run --project src/imgfnd -- --window "My App*" --image refs/ok-button.png
# output: 450,230

# with custom confidence threshold
dotnet run --project src/imgfnd -- -w "My App*" -i refs/icon.png --threshold 0.9
```

## Options

| Flag | Description | Default |
|------|-------------|---------|
| `-w, --window` | Window title pattern | required |
| `-i, --image` | Path to reference image (PNG, JPG, BMP) | required |
| `--threshold` | Match confidence 0.0–1.0 | 0.8 |

Outputs `x,y` coordinates on stdout (center of match). Pipeable to inpctl.

Reference images should be cropped screenshots of the element you want to find. Sensitive to DPI and theme changes.
