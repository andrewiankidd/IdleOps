# imgfnd — Image Template Matching

Find a UI element by matching a reference image against a window screenshot. Returns center coordinates.

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
