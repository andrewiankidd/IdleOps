# CLAUDE.md — scrcap

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Cross-platform CLI that screenshots a window (or the whole display via `--window screen`) and saves it as an image file. Windows uses GDI; Linux (X11) uses ImageMagick `import`; macOS uses `screencapture`.

## Key Types

| Type | Purpose |
|------|---------|
| `Options` | Record: Window, Output, ShowHelp, ShowVersion |
| `OptionsParser` | Manual CLI arg parsing |

The capture itself lives in `shared` (`IdleOps.Shared.Capture.IScreenCapturer` + `ScreenCapturerFactory`, shared with imgfnd and any future consumer). Program just resolves the capturer and calls it.

## Platform notes

- **Windows** 🟢 GDI PrintWindow/BitBlt (`WindowCapture`).
- **Linux/X11** 🟢 `import -window <id>` (needs ImageMagick on PATH); window id resolved via xdotool. Verified end-to-end (`scripts/linux-e2e.sh`).
- **macOS** 🟡 `screencapture` full-display only; per-window targeting by title needs a CoreGraphics window-id lookup (not wired up), so a specific `--window` warns and falls back to full screen.

## Dependencies

- NuGet: none (System.Drawing.Common comes transitively from shared, Windows path only)
- Project: shared (IScreenCapturer/factory, ConsoleLogger, HelpPrinter)
- Platform: Windows 🟢 / Linux-X11 🟢 / macOS 🟡 (full-screen)

## Build

```powershell
dotnet build src/scrcap/scrcap.csproj
```
