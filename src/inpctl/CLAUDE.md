# CLAUDE.md — inpctl

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Windows-only CLI for sending keyboard and mouse input to target windows via P/Invoke. Supports wildcard window title matching, key chords, text typing, and mouse clicks at absolute or percentage-based coordinates.

## Architecture

All logic lives in a single `Program.cs` (~822 lines) containing:

| Type | Purpose |
|------|---------|
| `Options` | Record: Window, Keyboard, Type, LeftMouse/RightMouse/MiddleMouse, Pid, SendCtrlC, MoveCursor |
| `WindowFinder` | `EnumWindows` + wildcard-to-regex matching. Picks most recently started process among matches. |
| `InputSender` | `PostMessage` for keyboard, `SendInput` for mouse. `VkKeyScan` for text typing with shifted characters. |

## Key Capabilities

- **Window matching**: Wildcard `*` patterns (prefix/suffix/infix) via regex
- **Keyboard**: Comma-separated key sequences, modifier combos (`CTRL+F4`, `ALT+TAB`). Recognizes: CTRL, ALT, SHIFT, ENTER, TAB, ESC, SPACE, BACKSPACE, arrows, F1-F12
- **Text typing**: Character-by-character via `VkKeyScan` (handles shifted chars)
- **Mouse**: Absolute pixel coords or percentage-based (`50%,50%`). Supports drag (`x1,y1-x2,y2`). Left/right/middle buttons. Optional `--move-cursor` to physically move the cursor.
- **CTRL+C to process**: `AttachConsole` + `GenerateConsoleCtrlEvent` via PID

## P/Invoke Surface

- `user32.dll`: EnumWindows, GetWindowText, GetWindowRect, IsWindowVisible, PostMessage, SendInput, VkKeyScan, SetCursorPos, GetForegroundWindow, SetForegroundWindow
- `kernel32.dll`: AttachConsole, FreeConsole, GenerateConsoleCtrlEvent, SetConsoleCtrlHandler

## Dependencies

- NuGet: none
- Project: none (standalone, no shared reference)
- Platform: Windows-only

## Build & Test

```powershell
dotnet build src/inpctl/inpctl.csproj
dotnet test src/inpctl.Tests/inpctl.Tests.csproj
```
