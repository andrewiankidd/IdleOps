# txtfnd — OCR Text Finder

Find text on screen and return its coordinates, enabling "find and click" automation.

## Usage

```bash
# find the "File" menu in Notepad
dotnet run --project src/txtfnd -- --window "Notepad*" --text "File"
# output: 25,12

# find and click in one line (bash)
dotnet run --project src/inpctl -- --window "Notepad*" --leftmouse \
  "$(dotnet run --project src/txtfnd -- -w 'Notepad*' -t 'File')"
```

## Options

| Flag | Description |
|------|-------------|
| `-w, --window <pattern>` | Window to screenshot (supports `*` wildcards) |
| `-t, --text <search>` | Text to find (case-insensitive substring) |
| `-h, --help` | Show help |
| `-v, --version` | Show version |

## Output

On success (exit code 0): prints `x,y` coordinates to stdout — the center of the found text, relative to the window's top-left corner. These coordinates can be passed directly to inpctl's `--leftmouse`.

On failure (exit code 1): prints recognized text to stderr for debugging, so you can see what OCR actually found.

## How It Works

1. Screenshots the target window using `PrintWindow` (works even for partially occluded windows)
2. Runs the built-in Windows OCR engine (`Windows.Media.Ocr`)
3. Searches all recognized words for the target text
4. Supports multi-word matches (e.g., "Save As" spanning two OCR words)
5. Returns the center point of the bounding rectangle

## Use in playbk Scripts

```yaml
steps:
  - name: Launch Notepad
    action: exec
    args: notepad.exe
    wait: false

  - name: Wait for window
    action: exec
    args: timeout /t 2
    wait: true

  - name: Click File menu
    action: exec
    args: >
      cmd /c "for /f %c in ('txtfnd -w \"*Notepad*\" -t \"File\"') do
      inpctl --window \"*Notepad*\" --leftmouse %c"
    wait: true
```

## Tips

- OCR works best on clearly rendered UI text at normal DPI
- If text is not found, check the stderr output to see what was recognized
- For small or anti-aliased text, ensure the window is not minimized
- The Windows OCR engine supports multiple languages — install language packs in Windows Settings
