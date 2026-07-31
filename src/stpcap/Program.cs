using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windows;
using stpcap.Recording;

[assembly: SupportedOSPlatform("windows")]

namespace stpcap;

internal static class Program
{
    private static int Main(string[] args)
    {
        string output = "recorded.idleops.yaml";
        string? windowFilter = null;
        bool showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                case "--output":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for output."); return 1; }
                    output = args[++i];
                    break;
                case "-w":
                case "--window":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for window."); return 1; }
                    windowFilter = args[++i];
                    break;
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return 1;
            }
        }

        if (showHelp)
        {
            Console.WriteLine("""
                Usage: stpcap [--output <file>] [--window "<filter>"]

                Record keyboard and mouse input into an IdleOps YAML script.
                Press Ctrl+C to stop recording.

                Options:
                  -o, --output    Output YAML file (default: recorded.idleops.yaml)
                  -w, --window    Only capture events for matching windows (optional)
                  -h, --help      Show help
                """);
            return 0;
        }

        if (!PlatformSupport.EnsureWindows("stpcap")) return 1;

        Console.WriteLine($"[stpcap] Recording input to '{output}'...");
        Console.WriteLine("[stpcap] Press Ctrl+C to stop and save.");
        if (windowFilter is not null)
        {
            Console.WriteLine($"[stpcap] Filtering to windows matching '{windowFilter}'");
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var recorder = new InputRecorder(windowFilter);
        recorder.Start();

        try
        {
            // Message pump — REQUIRED for low-level hooks (WH_MOUSE_LL / WH_KEYBOARD_LL):
            // the OS delivers their callbacks via this thread's message queue, so the
            // thread must retrieve messages or the hooks never fire. PeekMessage keeps
            // the loop responsive to Ctrl+C (a plain GetMessage would block indefinitely).
            while (!cts.IsCancellationRequested)
            {
                while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                Thread.Sleep(10);
            }
        }
        finally
        {
            recorder.Stop();
        }

        var events = recorder.Events;
        Console.WriteLine($"[stpcap] Captured {events.Count} events.");

        if (events.Count == 0)
        {
            Console.WriteLine("[stpcap] No events recorded.");
            return 0;
        }

        var yaml = ScriptGenerator.Generate(events);
        File.WriteAllText(output, yaml);
        Console.WriteLine($"[stpcap] Saved to '{output}'.");
        return 0;
    }

    // Message-pump P/Invoke — required so low-level hooks fire on this thread.
    private const uint PM_REMOVE = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG msg);
}
