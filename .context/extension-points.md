# Extension Points

## Adding a New Capture Platform

For audio (audcap) or video (vidcap):

1. Create a new class implementing `IAudioCapturer` or `IVideoCapturer` in the appropriate `Audio/` or `Video/` folder
2. If using ffmpeg, extend `FfmpegAudioCapturer` or `FfmpegVideoCapturer` and override the platform-specific args
3. Add a case to the factory (`AudioCapturerFactory` or `VideoCapturerFactory`) using `RuntimeInformation.IsOSPlatform`

Example pattern from `LinuxAudioCapturer`:
```csharp
internal sealed class LinuxAudioCapturer : FfmpegAudioCapturer
{
    protected override string InputFormat => "pulse";
    protected override string InputDevice => "default";
}
```

## Adding a New Script Action Type

Currently playbk only supports `action: exec`. To add a new action:

1. Add the action name as a case in `ScriptRunner.RunStep()` (in `src/playbk/Execution/ScriptRunner.cs`)
2. The `Step` model already has `Action` as a string field — no model changes needed
3. Implement the action logic, following the pattern of the `exec` handler (async, respects `step.Wait`, tracks PIDs if relevant)

## Adding New CLI Arguments

For any tool:

1. Add the property to the tool's `Options` record in `Cli/Options.cs`
2. Add parsing logic to `OptionsParser.Parse()` in `Cli/OptionsParser.cs`
3. Update `HelpFactory` to include the new argument in help output
4. Wire the option into the service layer

## Adding a New Tool

1. Create a new project under `src/` following the existing layout: `Cli/`, `Services/`, `Program.cs`
2. Reference `shared` for logging, platform detection, and help rendering
3. Add the project to `IdleOps.sln`
4. Add a test project `src/<tool>.Tests/` with xUnit references
5. If the tool should be available in playbk scripts, add MSBuild copy targets to `src/playbk/playbk.csproj`

## Adding Window Matching to Non-Windows Platforms

vidcap's `--window` flag is currently Windows-only (gdigrab + P/Invoke). To support it on other platforms:

- **macOS**: Use `screencapture` or CGWindowListCreateImage APIs
- **Linux**: Use `xdotool` for window ID lookup + `xwininfo` for geometry, then pass region to ffmpeg x11grab
