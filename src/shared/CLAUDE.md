# CLAUDE.md — shared

See also: [root CLAUDE.md](../../CLAUDE.md)

## Purpose

Shared class library providing cross-cutting utilities used by all IdleOps tools. Contains platform detection, console logging, CLI help rendering, capture result tracking, and Windows-specific window matching and screenshot capture.

## Key Namespaces

| Namespace | Key Types | Purpose |
|-----------|-----------|---------|
| `IdleOps.Shared.Platform` | `HostPlatform` enum, `HostInfo`, `PlatformSupport` | OS detection; `PlatformSupport.EnsureWindows` (CLI guard, `[SupportedOSPlatformGuard]`) / `RequireWindows` (throws `PlatformNotSupportedException`) for consistent Windows-only gaps |
| `IdleOps.Shared.Logging` | `ConsoleLogger` | Simple Info/Warn/Error to stdout/stderr |
| `IdleOps.Shared.Cli` | `HelpContent` record, `HelpPrinter` | Structured help data and formatted console output |
| `IdleOps.Shared.Capture` | `CaptureResult` record | OutputPath, StartTimeUtc, ExitCode for capture operations |
| `IdleOps.Shared.Windows` | `WindowMatcher`, `WindowCapture`, `WindowInfo`, `RECT` | Window enumeration, wildcard matching, screenshot capture (Windows-only) |
| `IdleOps.Shared.Windows.Uia` | `UiaAutomation`, `ElementInfo`, `ControlTypes`, `Selector` | Element automation via UI Automation — element-at-point, set-value/invoke/toggle/etc. Raw COM interop (internal), Windows-only. Shared by uiactl (CLI) and stpcap (recorder) |

## Windows Namespace Details

`IdleOps.Shared.Windows` consolidates P/Invoke window APIs previously duplicated across vidcap and inpctl:

- **`WindowMatcher`** — `BuildWildcardRegex(pattern)`, `FindWindow(pattern, preferNewest)`, `FindAllWindows(pattern)`, `GetWindowBounds(handle)`
- **`WindowCapture`** — `CaptureWindow(handle)` returns a `System.Drawing.Bitmap` via `PrintWindow` with `BitBlt` fallback
- **`WindowInfo`** — Record: Handle, Title, ProcessId
- **`RECT`** — Struct with Left, Top, Right, Bottom, Width, Height
- **`NativeMethods`** — Internal P/Invoke declarations for user32.dll and gdi32.dll

## Dependencies

- NuGet: System.Drawing.Common 9.0.5 (for WindowCapture bitmap operations)
- No project references (this is the base library)

## Build

```powershell
dotnet build src/shared/shared.csproj
dotnet test src/shared.Tests/shared.Tests.csproj
```
