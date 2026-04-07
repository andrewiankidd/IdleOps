using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using IdleOps.Shared.Logging;
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
            // Message pump — required for low-level hooks
            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(100);
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
}
