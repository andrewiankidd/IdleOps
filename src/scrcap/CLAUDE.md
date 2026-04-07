# CLAUDE.md — scrcap

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Simple Windows-only CLI that screenshots a window and saves it as an image file. Uses shared `WindowCapture` and `WindowMatcher`.

## Key Types

| Type | Purpose |
|------|---------|
| `Options` | Record: Window, Output, ShowHelp, ShowVersion |
| `OptionsParser` | Manual CLI arg parsing |

## Dependencies

- NuGet: none (System.Drawing.Common comes transitively from shared)
- Project: shared (WindowMatcher, WindowCapture, ConsoleLogger, HelpPrinter)
- Platform: Windows-only

## Build

```powershell
dotnet build src/scrcap/scrcap.csproj
```
