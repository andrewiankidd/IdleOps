# CLAUDE.md — inpctl

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform CLI for sending keyboard and mouse input to target windows. Supports wildcard window title matching, key chords, text typing, and mouse clicks at absolute or percentage-based coordinates. Windows uses user32 P/Invoke; Linux (X11) shells out to xdotool.

## Architecture

`Program.cs` is platform-agnostic orchestration; the OS-specific work sits behind an `IInputBackend` chosen by `InputBackendFactory` (mirrors audcap's `IAudioCapturer` factory).

| Type | Purpose |
|------|---------|
| `Options` | Record: Window, Keyboard, Type, LeftMouse/RightMouse/MiddleMouse, Pid, SendCtrlC, MoveCursor, Hold... |
| `IInputBackend` | Window targeting + management + input + interrupt. Handles are opaque `nint` (HWND / X11 window id). |
| `WindowsInputBackend` | user32/kernel32: SendInput/PostMessage input, window mgmt, console CTRL+C. Delegates key/mouse logic to `InputSender`. |
| `LinuxInputBackend` | xdotool for input + window control (X11), `kill -INT` for interrupt. `XdotoolKeys` translates chords to X11 keysyms. |
| `InputSender` | Windows keyboard/text via `SendInput` (foreground) or `PostMessage` (background); mouse via `SendInput`. |

## Key Capabilities

- **Window matching**: Wildcard `*` patterns (prefix/suffix/infix) via regex
- **Keyboard**: Comma-separated key sequences, modifier combos (`CTRL+F4`, `ALT+TAB`, `WIN+D`, `CTRL+SHIFT+ESC`). Modifiers are held during the key and released in reverse order. Recognizes: CTRL, ALT, SHIFT, WIN/LWIN/RWIN, APPS, ENTER, TAB, ESC, SPACE, BACKSPACE, INSERT, DELETE, CAPS, arrows, Home/End/PageUp/PageDown, F1-F12, and any single character. NOTE: `--ctrlc` is unrelated — it sends a console CTRL+C *signal* to a process by PID, not a keystroke.
- **Text typing**: Foreground via Unicode `SendInput` (layout-independent, drives WebView2/Chromium). `--background` posts `WM_CHAR` to the focused child control instead (no focus steal; classic Win32 only, not webviews)
- **Hold / sustained input**: `--hold "<keys>" [--duration <s>] [--method foreground|background] [--interval <ms>]`. Foreground holds via `SendInput` (OS keeps the key state pressed); background re-posts `WM_KEYDOWN` on an interval to the target window (a single posted key-down isn't auto-repeated). `InputMethod` enum is the extensible delivery qualifier. Runs until the duration elapses or Ctrl+C
- **Mouse**: Absolute pixel coords or percentage-based (`50%,50%`). Supports drag (`x1,y1-x2,y2`). Left/right/middle buttons. Optional `--move-cursor` to physically move the cursor.
- **CTRL+C to process**: `AttachConsole` + `GenerateConsoleCtrlEvent` via PID

## P/Invoke Surface

- `user32.dll`: EnumWindows, GetWindowText, GetWindowRect, IsWindowVisible, PostMessage, SendInput, VkKeyScan, SetCursorPos, GetForegroundWindow, SetForegroundWindow, GetGUIThreadInfo (resolve focused child for background posting)
- `kernel32.dll`: AttachConsole, FreeConsole, GenerateConsoleCtrlEvent, SetConsoleCtrlHandler

## Linux (X11) backend

- Requires **xdotool** on PATH (input + window targeting/move/resize/minimize). **wmctrl** is optional — used only for maximize/restore (falls back to a warning if absent).
- Wayland is out of scope: xdotool needs X11 (or XWayland-hosted windows), and Wayland has no global window addressing, so `--window` targeting requires an X session.
- Chords map to X11 keysyms via `XdotoolKeys` (e.g. `CTRL+S` -> `ctrl+s`, `WIN+D` -> `super+d`, `PAGEUP` -> `Prior`). Held keys use a single keydown/keyup pair (X auto-repeats at the server, so no re-post loop needed).
- **Delivery reliability**: foreground input (default) uses XTEST — real hardware-like events every app accepts; this is the reliable path (needs a window manager for `windowactivate` to focus). `--background` uses `xdotool --window` (XSendEvent), which many apps **ignore by default** for security (xterm's `allowSendEvents: false`, browsers, etc.), so background is best-effort on Linux — unlike Windows PostMessage there is no reliable no-focus-steal delivery.
- The pure translation logic (chord map, search regex, mouse-coord parsing) is unit-tested (`XdotoolKeysTests`); live injection is **verified end-to-end** on X11 under Xvfb (`scripts/linux-e2e.sh`, wired into the crossplatform CI job).

## Dependencies

- NuGet: none
- Project: `shared` (WindowMatcher/window bounds, Windows path only)
- Platform: Windows (user32) or Linux/X11 (xdotool). macOS not implemented.

## Build & Test

```powershell
dotnet build src/inpctl/inpctl.csproj
dotnet test src/inpctl.Tests/inpctl.Tests.csproj
```
