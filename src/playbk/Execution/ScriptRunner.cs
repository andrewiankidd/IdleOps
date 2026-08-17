using System.Diagnostics;
using System.Runtime.Versioning;
using IdleOps.Shared.Capture;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Ocr;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windowing;
using playbk.Model;
using vidcap.Services;
using audcap.Services;
using System.Text.RegularExpressions;
#if WINDOWS
using Microsoft.Win32;
using IdleOps.Shared.Win;
#endif
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace playbk.Execution;

internal sealed class ScriptRunner : IDisposable
{
    private readonly string _baseDir;
    private readonly string _outputDir;
    private readonly int _captureTimerSeconds;
    private readonly DeviceProfile _profile;
    private readonly Dictionary<string, int> _stepPids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Process> _stepProcesses = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> _backgroundTasks = [];
    // Warm OCR text finder, created on first click-text/assert-text and reused so
    // the OCR engine initializes once per run instead of once per step. Cross-platform:
    // WinRT OCR on Windows, Tesseract elsewhere (both via the shared ImageTextFinder).
    private ImageTextFinder? _textFinder;
    private readonly IScreenCapturer? _capturer = ScreenCapturerFactory.Create();
    private readonly IWindowLocator? _locator = WindowLocatorFactory.Create();

    public ScriptRunner(string baseDir, string outputDir, int captureTimerSeconds = 10, DeviceProfile? profile = null)
    {
        _baseDir = baseDir;
        _outputDir = outputDir;
        _captureTimerSeconds = captureTimerSeconds;
        _profile = profile ?? DeviceProfile.Local;
    }

    public void Dispose()
    {
        foreach (var process in _stepProcesses.Values)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
                process.Dispose();
            }
            catch
            {
                // best-effort cleanup
            }
        }
        _stepProcesses.Clear();
    }

    /// <summary>Wait for all fire-and-forget background tasks to complete.</summary>
    public Task DrainBackgroundTasksAsync() => Task.WhenAll(_backgroundTasks);

    public async Task<int> RunAsync(string scriptPath, CancellationToken token)
    {
        var script = LoadScript(scriptPath);
        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);

        // Pre-flight: reject action/profile mismatches before any step runs, so a
        // bad combination fails loudly and completely rather than mid-run.
        var violations = RunbookValidator.Validate(script, _profile);
        if (violations.Count > 0)
        {
            ConsoleLogger.Error(RunbookValidator.Format(violations, _profile));
            return 1;
        }

        Directory.CreateDirectory(_outputDir);

        // Optional media capture for the whole run. Each capturer runs on its own
        // timer (_captureTimerSeconds) in parallel with the steps; we await them
        // after the steps so the recording spans the flow.
        Task<CaptureResult>? videoTask = null;
        Task<CaptureResult>? audioTask = null;
        if (script.Vidcap)
        {
            var videoPath = Path.Combine(_outputDir, $"{Sanitize(scriptName)}-video.mp4");
            videoTask = VidcapService.CaptureAsync(videoPath, timerSeconds: _captureTimerSeconds, token: token);
        }
        if (script.Audcap)
        {
            var audioPath = Path.Combine(_outputDir, $"{Sanitize(scriptName)}-audio.wav");
            audioTask = AudcapService.CaptureAsync(audioPath, timerSeconds: _captureTimerSeconds, token: token);
        }

        var success = await RunStepsAsync(script, scriptName, token, scriptPath);

        foreach (var capture in new[] { videoTask, audioTask })
        {
            if (capture is null)
            {
                continue;
            }

            var result = await capture;
            if (result.ExitCode != 0)
            {
                ConsoleLogger.Warn($"Capture exited with code {result.ExitCode}: {result.OutputPath}");
            }
        }

        return success ? 0 : 1;
    }

    private async Task<bool> RunExecAsync(string commandOrUrl, bool wait, CancellationToken token, Step step)
    {
        if (string.IsNullOrWhiteSpace(commandOrUrl))
        {
            return true;
        }

        var expanded = ExpandPidTokens(commandOrUrl, _stepPids);
        expanded = NormalizeExecutable(expanded);
        var psi = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? $"/c {expanded}" : $"-c \"{expanded}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn($"Failed to start process: {psi.FileName} {psi.Arguments}");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(step.Id))
            {
                _stepPids[step.Id] = process.Id;
                _stepProcesses[step.Id] = process;
                ConsoleLogger.Info($"Step '{step.Id}' started (PID {process.Id}).");
            }

            if (wait)
            {
                try
                {
                    var stdOutTask = process.StandardOutput.ReadToEndAsync();
                    var stdErrTask = process.StandardError.ReadToEndAsync();
                    await Task.WhenAll(process.WaitForExitAsync(token), stdOutTask, stdErrTask);
                    var combined = CombineOutput(step.Name, stdOutTask.Result, stdErrTask.Result);
                    if (!string.IsNullOrWhiteSpace(combined))
                    {
                        Console.WriteLine(combined);
                    }
                }
                catch (InvalidOperationException)
                {
                    // process already exited; ignore
                }

                var failed = process.ExitCode != 0;
                if (failed)
                {
                    ConsoleLogger.Warn($"Process exited with code {process.ExitCode}: {psi.FileName} {psi.Arguments}");
                }

                process.Dispose();
                if (!string.IsNullOrWhiteSpace(step.Id))
                {
                    _stepProcesses.Remove(step.Id);
                }

                if (failed)
                {
                    return false;
                }
            }
            else
            {
                void StdOutHandler(object? sender, DataReceivedEventArgs e)
                {
                    if (e.Data is { Length: > 0 })
                    {
                        Console.WriteLine($"[{step.Name}] {e.Data}");
                    }
                }

                void StdErrHandler(object? sender, DataReceivedEventArgs e)
                {
                    if (e.Data is { Length: > 0 })
                    {
                        Console.WriteLine($"[{step.Name}][err] {e.Data}");
                    }
                }

                process.OutputDataReceived += StdOutHandler;
                process.ErrorDataReceived += StdErrHandler;
                try
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                catch (InvalidOperationException)
                {
                    // already exited; ignore
                }

                var bgTask = Task.Run(async () =>
                {
                    try
                    {
                        await process.WaitForExitAsync(CancellationToken.None);
                    }
                    catch (InvalidOperationException)
                    {
                        // already exited
                    }
                    finally
                    {
                        process.OutputDataReceived -= StdOutHandler;
                        process.ErrorDataReceived -= StdErrHandler;
                        process.Dispose();
                        if (!string.IsNullOrWhiteSpace(step.Id))
                        {
                            _stepProcesses.Remove(step.Id);
                        }
                    }
                });
                _backgroundTasks.Add(bgTask);
            }
        }
        catch (Exception ex)
        {
            ConsoleLogger.Warn($"Process failed: {ex.Message}");
            return false;
        }

        return true;
    }

    // --- Built-in actions ---

    private static async Task<bool> RunSleepAsync(Step step, CancellationToken token)
    {
        var seconds = step.Timeout ?? (double.TryParse(step.Args, out var parsed) ? parsed : 0);
        if (seconds <= 0)
        {
            ConsoleLogger.Warn("sleep: no duration specified (use args or timeout field).");
            return false;
        }

        ConsoleLogger.Info($"  Sleeping {seconds:0.##}s...");
        await Task.Delay(TimeSpan.FromSeconds(seconds), token);
        return true;
    }

    private async Task<bool> RunWaitWindowAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window ?? step.Args;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("wait-window: no window pattern specified.");
            return false;
        }

        var timeoutSeconds = step.Timeout ?? 10;

        // If text: is set, delegate to waitfr.exe so OCR polling actually runs.
        // The in-process handle-only path below silently ignores text:, which
        // misled consumers into thinking the wait would block on text appearing.
        if (!string.IsNullOrWhiteSpace(step.Text))
        {
            return await RunWaitWindowWithTextAsync(pattern, step.Text, timeoutSeconds, token);
        }

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        ConsoleLogger.Info($"  Waiting for window '{pattern}' (timeout {timeoutSeconds:0.#}s)...");
        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();
            if (_locator is not null && _locator.Exists(pattern))
            {
                ConsoleLogger.Info($"  Window '{pattern}' found.");
                return true;
            }

            await Task.Delay(250, token);
        }

        ConsoleLogger.Warn($"wait-window: timed out after {timeoutSeconds:0.#}s waiting for '{pattern}'.");
        return false;
    }

    private async Task<bool> RunWaitWindowWithTextAsync(string pattern, string text, double timeoutSeconds, CancellationToken token)
    {
        var waitfrPath = ResolveExecutable("waitfr") ?? ResolveExecutable("waitfr.exe");
        if (waitfrPath is null)
        {
            ConsoleLogger.Warn("wait-window: text: requires waitfr, but it was not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        ConsoleLogger.Info($"  Waiting for text '{text}' in window '{pattern}' (timeout {timeoutSeconds:0.#}s)...");

        var psi = new ProcessStartInfo
        {
            FileName = waitfrPath,
            Arguments = $"-w \"{pattern}\" -t \"{text}\" --timeout {timeoutSeconds:0.###}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn("wait-window: failed to start waitfr process.");
                return false;
            }

            var stderrTask = process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    ConsoleLogger.Info($"  {line.TrimEnd()}");
                }
            }

            if (process.ExitCode != 0)
            {
                ConsoleLogger.Warn($"wait-window: text '{text}' not found in window '{pattern}' within {timeoutSeconds:0.#}s.");
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"wait-window: waitfr failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> RunSpeakAsync(Step step, CancellationToken token)
    {
        var text = step.Text ?? step.Args;
        if (string.IsNullOrWhiteSpace(text))
        {
            ConsoleLogger.Warn("speak: no text specified (use text: or args).");
            return false;
        }

        var spkbakPath = ResolveExecutable("spkbak") ?? ResolveExecutable("spkbak.exe");
        if (spkbakPath is null)
        {
            ConsoleLogger.Warn("speak: spkbak not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        var args = new List<string> { "--text", $"\"{text.Replace("\"", "\\\"")}\"" };

        if (!string.IsNullOrWhiteSpace(step.Output))
        {
            var outputPath = Path.IsPathRooted(step.Output) ? step.Output : Path.Combine(_outputDir, step.Output);
            outputPath = Path.GetFullPath(outputPath);
            args.Add("--output");
            args.Add($"\"{outputPath}\"");
        }

        if (!string.IsNullOrWhiteSpace(step.Voice))
        {
            args.Add("--voice");
            args.Add($"\"{step.Voice}\"");
        }

        var psi = new ProcessStartInfo
        {
            FileName = spkbakPath,
            Arguments = string.Join(' ', args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        var preview = text.Length > 60 ? text[..60] + "..." : text;
        ConsoleLogger.Info($"  Speaking: \"{preview}\"{(step.Output is null ? "" : $" → {step.Output}")}");

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn("speak: failed to start spkbak process.");
                return false;
            }

            var stderrTask = process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    ConsoleLogger.Info($"  {line.TrimEnd()}");
                }
            }

            if (process.ExitCode != 0)
            {
                ConsoleLogger.Warn($"speak: spkbak exited with code {process.ExitCode}.");
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"speak: spkbak failed: {ex.Message}");
            return false;
        }
    }

    private bool RunScreenshot(Step step)
    {
        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("screenshot: no window pattern specified.");
            return false;
        }

        var outputPath = step.Output ?? step.Args;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            ConsoleLogger.Warn("screenshot: no output path specified.");
            return false;
        }

        outputPath = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(_outputDir, outputPath);
        outputPath = Path.GetFullPath(outputPath);

        if (_capturer is null)
        {
            ConsoleLogger.Warn("screenshot: no screen capturer for this OS.");
            return false;
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var outcome = _capturer.Capture(pattern, outputPath);
        if (!outcome.Ok)
        {
            ConsoleLogger.Warn($"screenshot: capture failed for window '{pattern}'.");
            return false;
        }

        ConsoleLogger.Info($"  Screenshot saved: {outputPath} ({outcome.Width}x{outcome.Height}, {sw.ElapsedMilliseconds}ms)");
        return true;
    }

    // WinRT OCR on Windows (zero-install, warm engine); Tesseract elsewhere.
    private static ITextRecognizer CreateRecognizer() =>
#if WINDOWS
        new WinRtTextRecognizer();
#else
        new TesseractTextRecognizer();
#endif

    // Single-shot OCR: locate `text` in `window` via the shared ImageTextFinder,
    // which reuses one warm OCR engine across steps (WinRT on Windows), returning
    // the window-relative "x,y" click coordinates on success, or null if not found /
    // on error (logged under `action` for context). Shared by click-text (find →
    // click) and assert-text (find → pass/fail). NOTE: wait-window's `text:` path
    // deliberately uses waitfr (which polls), not this single-shot path.
    private async Task<string?> FindTextViaOcrAsync(string window, string text, string action)
    {
        if (_capturer is null)
        {
            ConsoleLogger.Warn($"{action}: OCR unavailable (no screen capturer for this OS).");
            return null;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        ConsoleLogger.Info($"  OCR: searching for '{text}' in window '{window}'...");

        try
        {
            _textFinder ??= new ImageTextFinder(_capturer, CreateRecognizer());
            var coords = await _textFinder.FindAsync(window, text);
            if (coords is null)
            {
                ConsoleLogger.Warn($"{action}: text '{text}' not found in window '{window}' ({sw.ElapsedMilliseconds}ms).");
                return null;
            }

            var result = $"{coords.Value.x},{coords.Value.y}";
            ConsoleLogger.Info($"  OCR: found '{text}' at {result} ({sw.ElapsedMilliseconds}ms)");
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"{action}: OCR failed: {ex.GetType().Name} (0x{ex.HResult:X8}) {ex.Message}");
            return null;
        }
    }

    private async Task<bool> RunClickTextAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("click-text: no window pattern specified.");
            return false;
        }

        var searchText = step.Text;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            ConsoleLogger.Warn("click-text: no text specified.");
            return false;
        }

        var inpctlPath = ResolveExecutable("inpctl") ?? ResolveExecutable("inpctl.exe");
        if (inpctlPath is null)
        {
            ConsoleLogger.Warn("click-text: inpctl not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        // Phase 1: OCR — find text coordinates
        var coords = await FindTextViaOcrAsync(pattern, searchText, "click-text");
        if (coords is null)
        {
            return false;
        }

        // Phase 2: Click — send mouse input
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var clickPsi = new ProcessStartInfo
        {
            FileName = inpctlPath,
            Arguments = $"--window \"{pattern}\" --leftmouse {coords}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            using var clickProcess = Process.Start(clickPsi);
            if (clickProcess is null)
            {
                ConsoleLogger.Warn("click-text: failed to start inpctl process.");
                return false;
            }

            var clickStderr = await clickProcess.StandardError.ReadToEndAsync(token);
            await clickProcess.WaitForExitAsync(token);

            if (clickProcess.ExitCode != 0)
            {
                ConsoleLogger.Warn($"click-text: inpctl click failed (exit {clickProcess.ExitCode}, {sw.ElapsedMilliseconds}ms). stderr: {clickStderr.Trim()}");
                return false;
            }

            ConsoleLogger.Info($"  Click: sent to {coords} ({sw.ElapsedMilliseconds}ms)");
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"click-text: inpctl failed: {ex.Message}");
            return false;
        }
    }

    // Assert that OCR text is present in a window, without clicking - the read-only
    // sibling of click-text. Fails the step (and stops the flow) when the expected
    // text isn't found, so scripts can verify UI state. Use `wait-window` with a
    // `text:` first if the text needs time to appear.
    private async Task<bool> RunAssertTextAsync(Step step, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("assert-text: no window pattern specified.");
            return false;
        }

        var searchText = step.Text;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            ConsoleLogger.Warn("assert-text: no text specified.");
            return false;
        }

        // Same OCR find as click-text; assert-text just doesn't click. A null
        // result (text absent) fails the step and stops the flow.
        return await FindTextViaOcrAsync(pattern, searchText, "assert-text") is not null;
    }

    // Type text into a window via inpctl (a first-class alternative to
    // `exec inpctl --type`). The window must already have focus on the target
    // field; pair with a click-text on the field first if needed.
    private async Task<bool> RunTypeAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("type: no window pattern specified.");
            return false;
        }

        var text = step.Text ?? step.Args;
        if (string.IsNullOrWhiteSpace(text))
        {
            ConsoleLogger.Warn("type: no text specified.");
            return false;
        }

        var inpctlPath = ResolveExecutable("inpctl") ?? ResolveExecutable("inpctl.exe");
        if (inpctlPath is null)
        {
            ConsoleLogger.Warn("type: inpctl not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        var backgroundArg = step.Background ? " --background" : string.Empty;
        var psi = new ProcessStartInfo
        {
            FileName = inpctlPath,
            Arguments = $"--window \"{pattern}\" --type \"{text}\"{backgroundArg}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn("type: failed to start inpctl process.");
                return false;
            }

            var stderr = await process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);

            if (process.ExitCode != 0)
            {
                ConsoleLogger.Warn($"type: inpctl failed (exit {process.ExitCode}). stderr: {stderr.Trim()}");
                return false;
            }

            ConsoleLogger.Info($"  Typed into '{pattern}'.");
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"type: inpctl failed: {ex.Message}");
            return false;
        }
    }

    // Send a key chord / sequence into a window via inpctl (e.g. "CTRL+S",
    // "ALT+F4", "CTRL+A, DELETE"). The first-class equivalent of
    // `exec inpctl --keyboard`. Keys come from text: (or args:).
    private async Task<bool> RunKeyboardAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("keyboard: no window pattern specified.");
            return false;
        }

        var keys = step.Text ?? step.Args;
        if (string.IsNullOrWhiteSpace(keys))
        {
            ConsoleLogger.Warn("keyboard: no key(s) specified (use text: e.g. \"CTRL+S\").");
            return false;
        }

        var inpctlPath = ResolveExecutable("inpctl") ?? ResolveExecutable("inpctl.exe");
        if (inpctlPath is null)
        {
            ConsoleLogger.Warn("keyboard: inpctl not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        var backgroundArg = step.Background ? " --background" : string.Empty;
        var psi = new ProcessStartInfo
        {
            FileName = inpctlPath,
            Arguments = $"--window \"{pattern}\" --keyboard \"{keys}\"{backgroundArg}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn("keyboard: failed to start inpctl process.");
                return false;
            }

            var stderr = await process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);

            if (process.ExitCode != 0)
            {
                ConsoleLogger.Warn($"keyboard: inpctl failed (exit {process.ExitCode}). stderr: {stderr.Trim()}");
                return false;
            }

            ConsoleLogger.Info($"  Sent keys '{keys}' to '{pattern}'.");
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"keyboard: inpctl failed: {ex.Message}");
            return false;
        }
    }

    // --- UI Automation actions (element-level automation via uiactl) ----------
    // Resolve "--window ... <selector>" from the step. One selector: automation_id
    // wins, then element (accessibility Name), then control_type.
    private string? BuildUiaTarget(Step step, string action)
    {
        if (string.IsNullOrWhiteSpace(step.Window))
        {
            ConsoleLogger.Warn($"{action}: no window pattern specified.");
            return null;
        }

        string selector;
        if (!string.IsNullOrWhiteSpace(step.AutomationId))
            selector = $"--automation-id \"{step.AutomationId}\"";
        else if (!string.IsNullOrWhiteSpace(step.Element))
            selector = $"--name \"{step.Element}\"";
        else if (!string.IsNullOrWhiteSpace(step.ControlType))
            selector = $"--control-type \"{step.ControlType}\"";
        else
        {
            ConsoleLogger.Warn($"{action}: no selector (automation_id / element / control_type) specified.");
            return null;
        }

        return $"--window \"{step.Window}\" {selector}";
    }

    private ProcessStartInfo? BuildUiactlPsi(string arguments, string action)
    {
        var uiactlPath = ResolveExecutable("uiactl") ?? ResolveExecutable("uiactl.exe");
        if (uiactlPath is null)
        {
            ConsoleLogger.Warn($"{action}: uiactl not found on PATH. Ensure playbk is built with solution.");
            return null;
        }
        return new ProcessStartInfo
        {
            FileName = uiactlPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };
    }

    private async Task<bool> RunUiaVerbAsync(Step step, string verb, string? value, string action, CancellationToken token)
    {
        var target = BuildUiaTarget(step, action);
        if (target is null) return false;

        var args = value is not null ? $"{target} {verb} \"{value}\"" : $"{target} {verb}";
        var psi = BuildUiactlPsi(args, action);
        if (psi is null) return false;

        try
        {
            using var process = Process.Start(psi);
            if (process is null) { ConsoleLogger.Warn($"{action}: failed to start uiactl."); return false; }
            var stderr = await process.StandardError.ReadToEndAsync(token);
            var stdout = await process.StandardOutput.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            foreach (var line in (stderr + stdout).Split('\n', StringSplitOptions.RemoveEmptyEntries))
                ConsoleLogger.Info($"  {line.TrimEnd()}");
            if (process.ExitCode != 0) { ConsoleLogger.Warn($"{action}: uiactl exit {process.ExitCode}."); return false; }
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"{action}: uiactl failed: {ex.Message}");
            return false;
        }
    }

    // Reads the target element's value via uiactl and passes only if it equals step.Text.
    private async Task<bool> RunAssertValueAsync(Step step, CancellationToken token)
    {
        var expected = step.Text;
        if (expected is null)
        {
            ConsoleLogger.Warn("assert-value: no expected text specified.");
            return false;
        }
        var target = BuildUiaTarget(step, "assert-value");
        if (target is null) return false;
        var psi = BuildUiactlPsi($"{target} --get-value", "assert-value");
        if (psi is null) return false;

        try
        {
            using var process = Process.Start(psi);
            if (process is null) { ConsoleLogger.Warn("assert-value: failed to start uiactl."); return false; }
            var stdout = await process.StandardOutput.ReadToEndAsync(token);
            var stderr = await process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(stderr)) ConsoleLogger.Warn($"  {stderr.Trim()}");
                ConsoleLogger.Warn($"assert-value: uiactl exit {process.ExitCode}.");
                return false;
            }
            var actual = stdout.TrimEnd('\r', '\n');
            if (actual == expected)
            {
                ConsoleLogger.Info($"  assert-value: matched '{expected}'.");
                return true;
            }
            ConsoleLogger.Warn($"assert-value: expected '{expected}' but got '{actual}'.");
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"assert-value: uiactl failed: {ex.Message}");
            return false;
        }
    }

    // Hold key(s) down for a fixed duration via inpctl (text = the key(s)). A
    // runbook step must terminate, so a positive duration is required; use the
    // inpctl CLI directly for an indefinite (Ctrl+C) hold.
    private async Task<bool> RunHoldAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("hold: no window pattern specified.");
            return false;
        }

        var keys = step.Text;
        if (string.IsNullOrWhiteSpace(keys))
        {
            ConsoleLogger.Warn("hold: no key(s) specified (use text:).");
            return false;
        }

        var duration = step.Duration ?? 0;
        if (duration <= 0)
        {
            ConsoleLogger.Warn("hold: a positive duration is required in a runbook (use the inpctl CLI for an indefinite hold).");
            return false;
        }

        var inpctlPath = ResolveExecutable("inpctl") ?? ResolveExecutable("inpctl.exe");
        if (inpctlPath is null)
        {
            ConsoleLogger.Warn("hold: inpctl not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        var args = $"--window \"{pattern}\" --hold \"{keys}\" --duration {duration.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrWhiteSpace(step.Method)) args += $" --method {step.Method}";
        if (step.Interval is int interval) args += $" --interval {interval}";

        var psi = new ProcessStartInfo
        {
            FileName = inpctlPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null) { ConsoleLogger.Warn("hold: failed to start inpctl process."); return false; }
            var stderr = await process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            if (process.ExitCode != 0)
            {
                ConsoleLogger.Warn($"hold: inpctl failed (exit {process.ExitCode}). stderr: {stderr.Trim()}");
                return false;
            }
            ConsoleLogger.Info($"  Held '{keys}' on '{pattern}' for {duration:0.#}s.");
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"hold: inpctl failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> RunStepsAsync(Script script, string scriptName, CancellationToken token, string scriptPath)
    {
        foreach (var step in script.Steps)
        {
            token.ThrowIfCancellationRequested();
            ConsoleLogger.Info($"Step: {step.Name}");

            var ok = await RunStepWithRetryAsync(step, scriptPath, token);
            if (!ok)
            {
                if (step.ContinueOnError)
                {
                    ConsoleLogger.Warn($"Step '{step.Name}' failed; continuing (continue_on_error).");
                    continue;
                }

                ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                return false;
            }
        }

        return true;
    }

    // Runs a step, retrying on failure up to step.Retries additional attempts
    // (Ansible / AzureDevOps style). retries: 0 (default) preserves the original
    // fail-fast behaviour; retry_delay is the pause between attempts in seconds.
    private Task<bool> RunStepWithRetryAsync(Step step, string scriptPath, CancellationToken token)
        => RunWithRetryAsync(step.Retries, step.RetryDelay ?? 0, step.Name,
               () => DispatchStepAsync(step, scriptPath, token), token);

    // Retry an operation up to `retries` extra attempts (Ansible / AzureDevOps
    // semantics): total attempts = 1 + max(0, retries). Returns true on the first
    // success, false once attempts are exhausted. Pure control flow (the attempt is
    // injected), so it is unit-testable without driving real steps.
    internal static async Task<bool> RunWithRetryAsync(
        int retries, double delaySeconds, string label, Func<Task<bool>> attemptAsync, CancellationToken token)
    {
        var maxAttempts = 1 + Math.Max(0, retries);

        for (var attempt = 1; ; attempt++)
        {
            token.ThrowIfCancellationRequested();

            var ok = await attemptAsync();
            if (ok)
            {
                if (attempt > 1)
                    ConsoleLogger.Info($"  Step '{label}' succeeded on attempt {attempt}/{maxAttempts}.");
                return true;
            }

            if (attempt >= maxAttempts)
                return false;

            var when = delaySeconds > 0 ? $" retrying in {delaySeconds}s" : " retrying";
            ConsoleLogger.Warn($"  Step '{label}' failed (attempt {attempt}/{maxAttempts});{when}...");
            if (delaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
        }
    }

    private async Task<bool> DispatchStepAsync(Step step, string scriptPath, CancellationToken token)
    {
        switch (step.Action.ToLowerInvariant())
        {
            case "exec":
                return await RunExecAsync(step.Args ?? string.Empty, step.Wait, token, step);
            case "sleep":
                return await RunSleepAsync(step, token);
            case "wait-window":
                return await RunWaitWindowAsync(step, token);
            case "screenshot":
                return RunScreenshot(step);
            case "click-text":
                return await RunClickTextAsync(step, token);
            case "assert-text":
                return await RunAssertTextAsync(step, token);
            case "type":
                return await RunTypeAsync(step, token);
            case "keyboard" or "keys" or "chord":
                return await RunKeyboardAsync(step, token);
            case "speak":
                return await RunSpeakAsync(step, token);
            case "set-value":
                return await RunUiaVerbAsync(step, "--set-value", step.Text, "set-value", token);
            case "invoke":
                return await RunUiaVerbAsync(step, "--invoke", null, "invoke", token);
            case "toggle":
                return await RunUiaVerbAsync(step, "--toggle", null, "toggle", token);
            case "expand":
                return await RunUiaVerbAsync(step, "--expand", null, "expand", token);
            case "collapse":
                return await RunUiaVerbAsync(step, "--collapse", null, "collapse", token);
            case "select":
                return await RunUiaVerbAsync(step, "--select", null, "select", token);
            case "assert-value":
                return await RunAssertValueAsync(step, token);
            case "hold":
                return await RunHoldAsync(step, token);
            default:
                ConsoleLogger.Warn($"Unknown action '{step.Action}' in '{scriptPath}'.");
                return false;
        }
    }

    internal static string ExpandPidTokens(string input, IReadOnlyDictionary<string, int> stepPids)
    {
        return Regex.Replace(input, "%(?<id>[^%]+)_pid%", match =>
        {
            var id = match.Groups["id"].Value;
            return stepPids.TryGetValue(id, out var pid) ? pid.ToString() : match.Value;
        }, RegexOptions.IgnoreCase);
    }

    internal static (string fileName, string? arguments) SplitCommand(string command)
    {
        var tokens = Tokenize(command);
        if (tokens.Count == 0)
        {
            return (command, null);
        }

        var fileName = tokens[0];
        if (tokens.Count == 1)
        {
            return (fileName, null);
        }

        var args = string.Join(' ', tokens.Skip(1).Select(QuoteIfNeeded));
        return (fileName, args);
    }

    private static string NormalizeExecutable(string command)
    {
        var tokens = Tokenize(command);
        if (tokens.Count == 0)
        {
            return command;
        }

        var exe = tokens[0];
        var resolved = ResolveExecutable(exe);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            tokens[0] = resolved;
        }

        return string.Join(' ', tokens.Select(QuoteIfNeeded));
    }

    private static string? ResolveExecutable(string exe)
    {
        if (Path.IsPathRooted(exe) && File.Exists(exe))
        {
            return exe;
        }

        var candidateNames = new List<string> { exe };
        if (OperatingSystem.IsWindows() && !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            candidateNames.Add(exe + ".exe");
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var name in candidateNames)
        {
            foreach (var dir in paths)
            {
                var full = Path.Combine(dir, name);
                if (File.Exists(full))
                {
                    return full;
                }
            }
        }

#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            foreach (var name in candidateNames)
            {
                var fromAppPath = TryResolveFromAppPaths(name);
                if (!string.IsNullOrWhiteSpace(fromAppPath))
                {
                    return fromAppPath;
                }
            }
        }
#endif

        return null;
    }

#if WINDOWS
    // Windows "App Paths" registry lookup (a fallback for apps not on PATH).
    [SupportedOSPlatform("windows")]
    private static string? TryResolveFromAppPaths(string exeName)
    {
        var keys = new[]
        {
            Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + exeName),
            Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + exeName)
        };

        foreach (var key in keys)
        {
            using (key)
            {
                var value = key?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
                {
                    return value;
                }
            }
        }

        return null;
    }
#endif

    internal static List<string> Tokenize(string command)
    {
        var tokens = new List<string>();
        var pattern = @"[^\s""]+|""[^""]*""";
        foreach (Match m in Regex.Matches(command, pattern))
        {
            var token = m.Value;
            if (token.StartsWith("\"") && token.EndsWith("\""))
            {
                token = token.Substring(1, token.Length - 2);
            }
            tokens.Add(token);
        }
        return tokens;
    }

    internal static string QuoteIfNeeded(string token)
    {
        if (token.Contains(' ') || token.Contains('\t'))
        {
            return $"\"{token}\"";
        }

        return token;
    }

    internal static string CombineOutput(string stepName, string stdout, string stderr)
    {
        var sb = new List<string>();
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            foreach (var line in stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Add($"[{stepName}] {line}");
            }
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            foreach (var line in stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Add($"[{stepName}][err] {line}");
            }
        }

        return string.Join(Environment.NewLine, sb);
    }

    internal static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        cleaned = cleaned.Replace(' ', '_');
        return string.IsNullOrWhiteSpace(cleaned) ? "step" : cleaned;
    }

    private static Script LoadScript(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var script = deserializer.Deserialize<Script>(yaml) ?? new Script();
        return script;
    }
}


