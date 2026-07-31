#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using Windows.Media.SpeechSynthesis;

namespace spkbak.Speech;

[SupportedOSPlatform("windows10.0.22621.0")]
internal sealed class WindowsSpeechEngine : ISpeechEngine
{
    public string Name => "Windows.Media.SpeechSynthesis";

    public IReadOnlyList<string> ListVoices() =>
        SpeechSynthesizer.AllVoices.Select(v => $"{v.DisplayName} ({v.Language})").ToList();

    public async Task SpeakAsync(string text, string? voice, string? outputPath)
    {
        using var synth = new SpeechSynthesizer();

        if (voice is not null)
        {
            var match = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v => v.DisplayName.Contains(voice, StringComparison.OrdinalIgnoreCase));
            synth.Voice = match ?? throw new ArgumentException($"Voice '{voice}' not found. Use --list to see available voices.");
        }

        var stream = await synth.SynthesizeTextToStreamAsync(text);

        if (outputPath is not null)
        {
            using var fileStream = File.Create(outputPath);
            using var inputStream = stream.AsStreamForRead();
            await inputStream.CopyToAsync(fileStream);
        }
        else
        {
            var tempFile = Path.GetTempFileName() + ".wav";
            using (var fileStream = File.Create(tempFile))
            using (var inputStream = stream.AsStreamForRead())
            {
                await inputStream.CopyToAsync(fileStream);
            }
            using var player = new System.Media.SoundPlayer(tempFile);
            player.PlaySync();
            File.Delete(tempFile);
        }
    }
}
#endif
