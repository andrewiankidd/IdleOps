# inpctl — Input Control (Windows Only)

> **Platform:** 🟢 Windows · 🟡 Linux · 🟡 macOS  —  🟢 works · 🟡 stubbed (clear “not implemented” exit) · 🔴 not available

Send keyboard and mouse input to windows by title. Supports wildcard matching, key chords, text typing, and mouse clicks/drags.

## Usage

```bash
# type text into a window
dotnet run --project src/inpctl -- --window "Notepad*" --type "Hello, world!"

# send a key chord
dotnet run --project src/inpctl -- --window "My App*" --keyboard "CTRL+S"

# send a sequence of keys
dotnet run --project src/inpctl -- --keyboard "ALT+TAB,ENTER"

# click at absolute coordinates within a window
dotnet run --project src/inpctl -- --window "Paint*" --leftmouse "200,300"

# click at percentage-based coordinates
dotnet run --project src/inpctl -- --window "Paint*" --leftmouse "50%,50%"

# drag (left mouse from point A to point B)
dotnet run --project src/inpctl -- --window "Paint*" --leftmouse "30%,50%-70%,50%" --move-cursor

# send Ctrl+C to a process by PID
dotnet run --project src/inpctl -- --pid 12345 --ctrlc
```

## Options

| Flag | Description |
|------|-------------|
| `-w, --window <pattern>` | Target window by title (supports `*` wildcards) |
| `--keyboard <keys>` | Send key sequence (comma-separated, e.g., `CTRL+C,ALT+TAB`) |
| `--type <text>` | Type literal text (handles shifted characters automatically) |
| `--leftmouse <coords>` | Left-click at coordinates |
| `--rightmouse <coords>` | Right-click at coordinates |
| `--middlemouse <coords>` | Middle-click at coordinates |
| `--pid <pid>` | Target process ID (for `--ctrlc`) |
| `--ctrlc` | Send Ctrl+C signal to the target process |
| `--move-cursor` | Physically move the mouse cursor (default: input is injected without moving) |
| `--resize <WxH>` | Resize window (e.g., `1280x720` or `1280,720`) |
| `--move <x,y>` | Move window to screen position |
| `--maximize` | Maximize window |
| `--minimize` | Minimize window |
| `--restore` | Restore window from maximized/minimized |
| `-h, --help` | Show help |

## Window Matching

Wildcard patterns with `*` match any sequence of characters. Matching is case-insensitive. When multiple windows match, the most recently started process is selected.

Examples: `Notepad*`, `*Chrome*`, `My*App`, `*Untitled*Notepad`

## Coordinate Formats

| Format | Example | Description |
|--------|---------|-------------|
| Absolute | `200,300` | Pixel coordinates relative to window top-left |
| Percentage | `50%,50%` | Percentage of window width/height |
| Screen percentage | `10%,90%` | Without `--window`, percentage of full screen |
| Drag | `30%,50%-70%,50%` | Mouse down at first point, up at second |

## Supported Keys

Modifiers: `CTRL` (`CONTROL`), `ALT`, `SHIFT`

Keys: `ENTER` (`RETURN`), `TAB`, `ESC` (`ESCAPE`), `SPACE`, `BACKSPACE`, `DELETE`, `UP`, `DOWN`, `LEFT`, `RIGHT`, `HOME`, `END`, `PAGEUP`, `PAGEDOWN`, `F1`–`F12`

Combine with `+`: `CTRL+C`, `ALT+F4`, `CTRL+SHIFT+S`
