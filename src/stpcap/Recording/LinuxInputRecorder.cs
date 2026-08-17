using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleOps.Shared.Windows;

namespace stpcap.Recording;

/// <summary>
/// Linux backend: records X input via the bundled stprec_helper.py (XRecord). Reads
/// one JSON event per line, coalescing printable keys into typed-text steps and
/// mapping special keys onto the VK names ScriptGenerator understands. No AT-SPI
/// element capture yet, so clicks record as window-relative coordinates.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxInputRecorder : IInputRecorder
{
    private readonly Regex? _filter;
    private readonly List<InputEvent> _events = [];
    private readonly StringBuilder _typing = new();
    private string? _typingWindow;
    private Process? _proc;

    // X keysym name -> Windows VK, so ScriptGenerator.VkToName emits inpctl key names.
    private static readonly Dictionary<string, ushort> SpecialVk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Return"] = 0x0D, ["KP_Enter"] = 0x0D, ["Tab"] = 0x09, ["BackSpace"] = 0x08,
        ["Escape"] = 0x1B, ["Delete"] = 0x2E, ["Home"] = 0x24, ["End"] = 0x23,
        ["Left"] = 0x25, ["Up"] = 0x26, ["Right"] = 0x27, ["Down"] = 0x28,
        ["Prior"] = 0x21, ["Next"] = 0x22,
        ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73, ["F5"] = 0x74, ["F6"] = 0x75,
        ["F7"] = 0x76, ["F8"] = 0x77, ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B,
    };

    public LinuxInputRecorder(string? windowFilter) =>
        _filter = windowFilter is not null ? WindowMatcher.BuildWildcardRegex(windowFilter) : null;

    public string Name => "xrecord (X11)";
    public IReadOnlyList<InputEvent> Events => _events;

    public void RunUntil(CancellationToken token)
    {
        var helper = Path.Combine(AppContext.BaseDirectory, "stprec_helper.py");
        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(helper);

        try { _proc = Process.Start(psi); }
        catch (Exception ex) { Console.Error.WriteLine($"[stpcap] failed to start recorder helper: {ex.Message} (install python3-xlib)"); return; }
        if (_proc is null) { Console.Error.WriteLine("[stpcap] failed to start recorder helper."); return; }

        var reader = new Thread(() => ReadLoop(_proc)) { IsBackground = true };
        reader.Start();

        // Block until cancelled, then close the helper's stdin so it stops cleanly.
        try { while (!token.IsCancellationRequested) Thread.Sleep(50); }
        finally
        {
            try { _proc.StandardInput.Close(); } catch { }
            try { if (!_proc.WaitForExit(2000)) _proc.Kill(); } catch { }
            reader.Join(2000);
            FlushTyping();
        }
    }

    private void ReadLoop(Process proc)
    {
        string? line;
        while ((line = proc.StandardOutput.ReadLine()) is not null)
        {
            Ev? e;
            try { e = JsonSerializer.Deserialize<Ev>(line); }
            catch { continue; }
            if (e is null) continue;

            var win = string.IsNullOrEmpty(e.win) ? null : e.win;
            if (_filter is not null && (win is null || !_filter.IsMatch(win))) continue;

            if (e.t == "button")
            {
                FlushTyping();
                var button = e.detail == 3 ? "right" : e.detail == 2 ? "middle" : "left";
                Add(new InputEvent(InputEventType.MouseClick, Now(), win, X: e.x, Y: e.y, Button: button));
            }
            else if (e.t == "key")
            {
                if (e.kind == "char" && !string.IsNullOrEmpty(e.sym))
                {
                    if (_typing.Length > 0 && _typingWindow != win) FlushTyping();
                    _typingWindow = win;
                    _typing.Append(e.sym);
                }
                else if (SpecialVk.TryGetValue(e.sym ?? "", out var vk))
                {
                    FlushTyping();
                    Add(new InputEvent(InputEventType.KeyDown, Now(), win, VirtualKey: vk));
                }
            }
        }
    }

    private void FlushTyping()
    {
        if (_typing.Length == 0) return;
        Add(new InputEvent(InputEventType.TextInput, Now(), _typingWindow, Button: _typing.ToString()));
        _typing.Clear();
        _typingWindow = null;
    }

    private void Add(InputEvent e) { lock (_events) _events.Add(e); }
    private static DateTime Now() => DateTime.UtcNow;

    public void Dispose() { try { _proc?.Dispose(); } catch { } }

    private sealed record Ev(string t, int detail, int x, int y, string? sym, string? kind, string? win);
}
