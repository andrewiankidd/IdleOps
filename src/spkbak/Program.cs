using System.Runtime.Versioning;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

[assembly: SupportedOSPlatform("windows10.0.22621.0")]

namespace spkbak;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        string? text = null;
        string? file = null;
        string? output = null;
        string? voice = null;
        bool list = false;
        bool showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                case "--text":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for text."); return 1; }
                    text = args[++i];
                    break;
                case "-f":
                case "--file":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for file."); return 1; }
                    file = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for output."); return 1; }
                    output = args[++i];
                    break;
                case "--voice":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value for voice."); return 1; }
                    voice = args[++i];
                    break;
                case "--list":
                    list = true;
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
                Usage: spkbak --text "<text>" [--output <file.wav>] [--voice "<name>"]

                Text-to-speech: speak text aloud or save to WAV file.

                Options:
                  -t, --text      Text to speak
                  -f, --file      Read text from file instead
                  -o, --output    Save speech to WAV file (instead of playing)
                  --voice         Voice name (use --list to see available)
                  --list          List available voices
                  -h, --help      Show help
                """);
            return 0;
        }

        using var synth = new SpeechSynthesizer();

        if (list)
        {
            foreach (var v in SpeechSynthesizer.AllVoices)
            {
                var marker = v.Id == synth.Voice.Id ? " *" : "";
                Console.WriteLine($"  {v.DisplayName} ({v.Language}){marker}");
            }
            return 0;
        }

        // Read text from file if specified
        if (file is not null)
        {
            if (!File.Exists(file)) { Console.Error.WriteLine($"File not found: {file}"); return 1; }
            text = await File.ReadAllTextAsync(file);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine("No text specified. Use --text or --file.");
            return 1;
        }

        // Set voice
        if (voice is not null)
        {
            var match = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                synth.Voice = match;
            }
            else
            {
                Console.Error.WriteLine($"Voice '{voice}' not found. Use --list to see available voices.");
                return 1;
            }
        }

        Console.Error.WriteLine($"[spkbak] Voice: {synth.Voice.DisplayName}");
        Console.Error.WriteLine($"[spkbak] Text: {(text.Length > 80 ? text[..80] + "..." : text)}");

        try
        {
            var stream = await synth.SynthesizeTextToStreamAsync(text);

            if (output is not null)
            {
                // Save to WAV file
                var outputPath = Path.GetFullPath(output);
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

                using var fileStream = File.Create(outputPath);
                using var inputStream = stream.AsStreamForRead();
                await inputStream.CopyToAsync(fileStream);

                Console.Error.WriteLine($"[spkbak] Saved to {outputPath}");
                Console.WriteLine(outputPath);
            }
            else
            {
                // Play through speakers using Windows.Media.Playback
                // Simple approach: save to temp file, play with SoundPlayer
                var tempFile = Path.GetTempFileName() + ".wav";
                using (var fileStream = File.Create(tempFile))
                using (var inputStream = stream.AsStreamForRead())
                {
                    await inputStream.CopyToAsync(fileStream);
                }

                using var player = new System.Media.SoundPlayer(tempFile);
                player.PlaySync();
                File.Delete(tempFile);

                Console.Error.WriteLine("[spkbak] Playback complete.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[spkbak] Failed: {ex.Message}");
            return 1;
        }
    }
}
