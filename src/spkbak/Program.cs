using spkbak.Speech;

namespace spkbak;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        spkbak.Cli.Options options;
        try
        {
            options = spkbak.Cli.OptionsParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var text = options.Text;
        var file = options.File;
        var output = options.Output;
        var voice = options.Voice;
        var list = options.List;
        var showHelp = options.ShowHelp;

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

        var engine = SpeechEngineFactory.Create();

        if (list)
        {
            foreach (var v in engine.ListVoices())
            {
                Console.WriteLine($"  {v}");
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

        string? outputPath = null;
        if (output is not null)
        {
            outputPath = Path.GetFullPath(output);
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        }

        Console.Error.WriteLine($"[spkbak] Engine: {engine.Name}");
        Console.Error.WriteLine($"[spkbak] Text: {(text.Length > 80 ? text[..80] + "..." : text)}");

        try
        {
            await engine.SpeakAsync(text, voice, outputPath);

            if (outputPath is not null)
            {
                Console.Error.WriteLine($"[spkbak] Saved to {outputPath}");
                Console.WriteLine(outputPath);
            }
            else
            {
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
