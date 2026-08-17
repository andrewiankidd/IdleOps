using System.Runtime.InteropServices;
using stpcap.Recording;

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
            Console.WriteLine(IdleOps.Shared.Cli.BuildInfo.Banner("stpcap"));
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

        var recorder = InputRecorderFactory.Create(windowFilter);
        if (recorder is null)
        {
            Console.Error.WriteLine("[stpcap] no input recorder for this OS (supported: Windows hooks, Linux/X11 XRecord).");
            return 1;
        }

        Console.WriteLine($"[stpcap] Recording input to '{output}' ({recorder.Name})...");
        Console.WriteLine("[stpcap] Press Ctrl+C to stop and save.");
        if (windowFilter is not null)
        {
            Console.WriteLine($"[stpcap] Filtering to windows matching '{windowFilter}'");
        }

        using var cts = new CancellationTokenSource();
        // PosixSignalRegistration fires for Ctrl+C AND `kill` on both Windows and
        // Linux (Console.CancelKeyPress misses SIGINT sent to a no-TTY process).
        using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => { ctx.Cancel = true; cts.Cancel(); });
        using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; cts.Cancel(); });

        using (recorder)
        {
            recorder.RunUntil(cts.Token);
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
