using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace vidcap.Tests;

public class RecordingTests
{
    [Fact]
    public void DesktopCaptureProducesVideo()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var vidcapExe = FindVidcapExe();
        Assert.False(string.IsNullOrWhiteSpace(vidcapExe), "vidcap.exe not found; build vidcap first.");

        var desktopFile = CreateTempOutput(".desktop.mp4");
        const int timerSeconds = 4;

        using var capture = StartProcess(vidcapExe!, $"--output \"{desktopFile}\" --timer {timerSeconds}");
        capture.WaitForExit(timerSeconds * 2000);

        Assert.True(File.Exists(desktopFile), "Desktop capture not created.");
        Assert.True(new FileInfo(desktopFile).Length > 10_000, "Desktop capture file is empty.");
        var res = ProbeResolution(desktopFile);
        Assert.True(res.width > 0 && res.height > 0, "Desktop capture resolution unavailable.");
    }

    [Fact]
    public void WindowCaptureIsSmallerThanDesktop()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var vidcapExe = FindVidcapExe();
        Assert.False(string.IsNullOrWhiteSpace(vidcapExe), "vidcap.exe not found; build vidcap first.");

        var desktopFile = CreateTempOutput(".desktop.mp4");
        var windowFile = CreateTempOutput(".window.mp4");
        const int timerSeconds = 6;

        using var notepad = StartProcess("notepad.exe", "");
        var (handle, title) = WaitForNotepadWindow(notepad, TimeSpan.FromSeconds(15));
        MoveWindow(handle, 50, 50, 600, 400, true);

        using var desktopCapture = StartProcess(vidcapExe!, $"--output \"{desktopFile}\" --timer {timerSeconds}");

        // Give desktop capture a head start.
        Thread.Sleep(2000);

        using var windowCapture = StartProcess(vidcapExe!, $"--output \"{windowFile}\" --delay 0.5 --timer {timerSeconds} --window \"{title}\"");

        SendTextToWindow(handle, title, "vidcap window capture test");
        Thread.Sleep(2000);

        desktopCapture.WaitForExit(timerSeconds * 2000);
        windowCapture.WaitForExit(timerSeconds * 2000);

        try
        {
            notepad.CloseMainWindow();
            notepad.WaitForExit(2000);
        }
        finally
        {
            if (!notepad.HasExited)
            {
                notepad.Kill(true);
            }
        }

        Assert.True(File.Exists(desktopFile), "Desktop capture not created.");
        Assert.True(File.Exists(windowFile), "Window capture not created.");
        Assert.True(new FileInfo(desktopFile).Length > 10_000, "Desktop capture file is empty.");
        Assert.True(new FileInfo(windowFile).Length > 2_000, "Window capture file is empty.");

        var desktopRes = ProbeResolution(desktopFile);
        var windowRes = ProbeResolution(windowFile);
        Assert.True(desktopRes.width > windowRes.width && desktopRes.height > windowRes.height,
            $"Expected window capture to be smaller. Desktop: {desktopRes.width}x{desktopRes.height}, Window: {windowRes.width}x{windowRes.height}");
    }

    private static Process StartProcess(string fileName, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        return process;
    }

    private static string? FindVidcapExe()
    {
        var binRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "vidcap", "bin"));
        return Directory.Exists(binRoot)
            ? Directory.EnumerateFiles(binRoot, "vidcap.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private static string CreateTempOutput(string suffix)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vidcap-tests");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, $"capture-{Guid.NewGuid():N}{suffix}");
    }

    private static (IntPtr Handle, string Title) WaitForNotepadWindow(Process process, TimeSpan timeout)
    {
        try
        {
            process.WaitForInputIdle((int)timeout.TotalMilliseconds);
        }
        catch
        {
            // ignore
        }

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                var title = !string.IsNullOrWhiteSpace(process.MainWindowTitle)
                    ? process.MainWindowTitle
                    : GetWindowText(process.MainWindowHandle);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return (process.MainWindowHandle, title);
                }
            }

            var altHandle = FindWindow("Notepad", null);
            if (altHandle != IntPtr.Zero)
            {
                var altTitle = GetWindowText(altHandle);
                if (!string.IsNullOrWhiteSpace(altTitle))
                {
                    return (altHandle, altTitle);
                }
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException($"Notepad window not found within {timeout.TotalSeconds} seconds.");
    }

    private static void SendTextToWindow(IntPtr handle, string windowTitle, string text)
    {
        if (handle == IntPtr.Zero)
        {
            handle = FindWindow(null, windowTitle);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Window '{windowTitle}' not found.");
            }
        }

        SetForegroundWindow(handle);
        Thread.Sleep(400);

        foreach (var ch in text)
        {
            var input = new INPUT
            {
                type = 1, // INPUT_KEYBOARD
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = KEYEVENTF.UNICODE
                    }
                }
            };

            var up = input;
            up.U.ki.dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP;
            SendInput(2, new[] { input, up }, Marshal.SizeOf<INPUT>());
        }
    }

    private static (int width, int height) ProbeResolution(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0:s=x \"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            return (0, 0);
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        var parts = output.Trim().Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
        {
            return (w, h);
        }

        return (0, 0);
    }

    #region Native
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public KEYEVENTF dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [Flags]
    private enum KEYEVENTF : uint
    {
        KEYDOWN = 0x0000,
        EXTENDEDKEY = 0x0001,
        KEYUP = 0x0002,
        UNICODE = 0x0004,
        SCANCODE = 0x0008
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    private static string GetWindowText(IntPtr hWnd)
    {
        var chars = new char[512];
        var length = GetWindowText(hWnd, chars, chars.Length);
        return length > 0 ? new string(chars, 0, length) : string.Empty;
    }
    #endregion
}
