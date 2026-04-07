using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windows;
using playbk.Model;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace playbk.Execution;

public sealed class ScriptRunner : IDisposable
{
    private readonly string _baseDir;
    private readonly string _outputDir;
    private readonly int _captureTimerSeconds;
    private readonly Dictionary<string, int> _stepPids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Process> _stepProcesses = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> _backgroundTasks = [];

    public ScriptRunner(string baseDir, string outputDir, int captureTimerSeconds = 10)
    {
        _baseDir = baseDir;
        _outputDir = outputDir;
        _captureTimerSeconds = captureTimerSeconds;
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

        Directory.CreateDirectory(_outputDir);

        var success = await RunStepsAsync(script, scriptName, token, scriptPath);

        return success ? 0 : 1;
    }

    private async Task<bool> RunExecAsync(string commandOrUrl, bool wait, CancellationToken token, Step step)
    {
        if (string.IsNullOrWhiteSpace(commandOrUrl))
        {
            return true;
        }

        var expanded = ExpandPidTokens(commandOrUrl);
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

    private static async Task<bool> RunWaitWindowAsync(Step step, CancellationToken token)
    {
        var pattern = step.Window ?? step.Args;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleLogger.Warn("wait-window: no window pattern specified.");
            return false;
        }

        var timeoutSeconds = step.Timeout ?? 10;
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        ConsoleLogger.Info($"  Waiting for window '{pattern}' (timeout {timeoutSeconds:0.#}s)...");
        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();
            if (WindowMatcher.FindWindow(pattern) is not null)
            {
                ConsoleLogger.Info($"  Window '{pattern}' found.");
                return true;
            }

            await Task.Delay(250, token);
        }

        ConsoleLogger.Warn($"wait-window: timed out after {timeoutSeconds:0.#}s waiting for '{pattern}'.");
        return false;
    }

    [SupportedOSPlatform("windows")]
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

        var match = WindowMatcher.FindWindow(pattern, preferNewest: true);
        if (match is null)
        {
            ConsoleLogger.Warn($"screenshot: window '{pattern}' not found.");
            return false;
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var bitmap = WindowCapture.CaptureWindow(match.Handle);
            var ext = Path.GetExtension(outputPath).ToLowerInvariant();
            var format = ext switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png
            };
            bitmap.Save(outputPath, format);
            ConsoleLogger.Info($"  Screenshot saved: {outputPath} ({bitmap.Width}x{bitmap.Height}, {sw.ElapsedMilliseconds}ms)");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Warn($"screenshot: capture failed for window '{pattern}': {ex.Message}");
            return false;
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

        var txtfndPath = ResolveExecutable("txtfnd") ?? ResolveExecutable("txtfnd.exe");
        if (txtfndPath is null)
        {
            ConsoleLogger.Warn("click-text: txtfnd not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        var inpctlPath = ResolveExecutable("inpctl") ?? ResolveExecutable("inpctl.exe");
        if (inpctlPath is null)
        {
            ConsoleLogger.Warn("click-text: inpctl not found on PATH. Ensure playbk is built with solution.");
            return false;
        }

        // Phase 1: OCR — find text coordinates
        var sw = System.Diagnostics.Stopwatch.StartNew();
        ConsoleLogger.Info($"  OCR: searching for '{searchText}' in window '{pattern}'...");

        var psi = new ProcessStartInfo
        {
            FileName = txtfndPath,
            Arguments = $"-w \"{pattern}\" -t \"{searchText}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputDir
        };

        string coords;
        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleLogger.Warn("click-text: failed to start txtfnd process.");
                return false;
            }

            // Hard timeout: kill txtfnd if it doesn't finish in 15 seconds
            if (!process.WaitForExit(15000))
            {
                ConsoleLogger.Warn($"click-text: txtfnd timed out after 15s searching for '{searchText}'. Killing process.");
                process.Kill(true);
                return false;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    ConsoleLogger.Info($"  {line.TrimEnd()}");
                }
            }

            coords = stdout.Trim();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(coords))
            {
                ConsoleLogger.Warn($"click-text: text '{searchText}' not found in window '{pattern}' (txtfnd exit {process.ExitCode}, {sw.ElapsedMilliseconds}ms).");
                return false;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ConsoleLogger.Warn($"click-text: txtfnd failed: {ex.Message}");
            return false;
        }

        ConsoleLogger.Info($"  OCR: found '{searchText}' at {coords} ({sw.ElapsedMilliseconds}ms)");

        // Phase 2: Click — send mouse input
        sw.Restart();
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

    private async Task<bool> RunStepsAsync(Script script, string scriptName, CancellationToken token, string scriptPath)
    {
        foreach (var step in script.Steps)
        {
            token.ThrowIfCancellationRequested();
            ConsoleLogger.Info($"Step: {step.Name}");
            switch (step.Action.ToLowerInvariant())
            {
                case "exec":
                    {
                        var ok = await RunExecAsync(step.Args ?? string.Empty, step.Wait, token, step);
                        if (!ok)
                        {
                            ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                            return false;
                        }
                        break;
                    }
                case "sleep":
                    {
                        var ok = await RunSleepAsync(step, token);
                        if (!ok)
                        {
                            ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                            return false;
                        }
                        break;
                    }
                case "wait-window":
                    {
                        var ok = await RunWaitWindowAsync(step, token);
                        if (!ok)
                        {
                            ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                            return false;
                        }
                        break;
                    }
                case "screenshot" when OperatingSystem.IsWindows():
                    {
                        var ok = RunScreenshot(step);
                        if (!ok)
                        {
                            ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                            return false;
                        }
                        break;
                    }
                case "click-text":
                    {
                        var ok = await RunClickTextAsync(step, token);
                        if (!ok)
                        {
                            ConsoleLogger.Error($"Step '{step.Name}' failed; stopping flow.");
                            return false;
                        }
                        break;
                    }
                default:
                    ConsoleLogger.Warn($"Unknown action '{step.Action}' in '{scriptPath}'.");
                    return false;
            }
        }

        return true;
    }

    private string ExpandPidTokens(string input)
    {
        return Regex.Replace(input, "%(?<id>[^%]+)_pid%", match =>
        {
            var id = match.Groups["id"].Value;
            return _stepPids.TryGetValue(id, out var pid) ? pid.ToString() : match.Value;
        }, RegexOptions.IgnoreCase);
    }

    private static (string fileName, string? arguments) SplitCommand(string command)
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

        return null;
    }

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

    private static List<string> Tokenize(string command)
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

    private static string QuoteIfNeeded(string token)
    {
        if (token.Contains(' ') || token.Contains('\t'))
        {
            return $"\"{token}\"";
        }

        return token;
    }

    private static string CombineOutput(string stepName, string stdout, string stderr)
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

    private static string Sanitize(string name)
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


