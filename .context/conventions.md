# Conventions

## Project Layout

- `Cli/` — Options record, OptionsParser (uses shared `ArgParser`), HelpFactory
- Domain folders by concern — `Audio/`, `Video/`, `Input/`, `Ocr/`, `Recording/`, `Execution/`, `Model/`
- `Program.cs` — thin entry point, orchestration only

## Type Patterns

- **Records** for immutable data (Options, CaptureResult, HelpContent, Script, Step)
- **Classes** for stateful services (ScriptRunner, CaptureRunner, InputSender, InputRecorder)
- **Sealed** on implementations where inheritance is not intended

## CLI

- All tools use `IdleOps.Shared.Cli.ArgParser` for argument parsing
- Arguments follow `-short` / `--long` conventions
- All tools support `-h, --help` and `-v, --version`
- Tools that produce machine-readable output (txtfnd, scrcap, imgfnd, cnvrtr) write it to stdout; status messages go to stderr

## Async

- Capture operations return `Task` or `Task<CaptureResult>` with `CancellationToken`
- `Process.WaitForExitAsync` for non-blocking process waits
- `Task.WhenAll` for parallel operations (outcap A/V capture)

## Platform

- `RuntimeInformation.IsOSPlatform` or factory methods for platform-specific implementations
- Windows-only tools use `[SupportedOSPlatform("windows")]`
- Shared Windows P/Invoke consolidated in `IdleOps.Shared.Windows.NativeMethods`

## Naming

- **Projects**: lowercase abbreviated, 5-6 chars (audcap, vidcap, txtfnd, scrcap, playbk, inpctl, waitfr, imgfnd, stpcap, spkbak, cnvrtr)
- **Namespaces**: Tool-specific = flat namespace matching project name. Shared = `IdleOps.Shared.*`
- **YAML**: snake_case fields, deserialized to PascalCase via YamlDotNet `UnderscoredNamingConvention`
