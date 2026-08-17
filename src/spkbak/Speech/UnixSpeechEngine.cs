using System.Diagnostics;

namespace spkbak.Speech;

/// <summary>
/// macOS / Linux TTS by shelling out to the platform speech CLI: `say` on macOS,
/// `espeak` on Linux. Text is passed as a process argument (via ArgumentList) so
/// arbitrary content needs no shell escaping.
/// </summary>
internal sealed class UnixSpeechEngine : ISpeechEngine
{
    private readonly bool _mac = OperatingSystem.IsMacOS();

    public string Name => _mac ? "say" : "espeak";

    public async Task SpeakAsync(string text, string? voice, string? outputPath)
    {
        var psi = new ProcessStartInfo { FileName = _mac ? "say" : "espeak", UseShellExecute = false };

        if (voice is not null)
        {
            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add(voice);
        }

        if (outputPath is not null)
        {
            if (_mac)
            {
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(outputPath);
                // `--file-format=WAVE` is rejected outright ("Opening output file failed:
                // fmt?"), as is a bare .wav path — `say` picks the container from the
                // extension but needs an explicit PCM data format to go with it. This one
                // is `say`'s standard 16-bit LE PCM and yields a normal RIFF/WAVE file.
                psi.ArgumentList.Add("--data-format=LEI16@22050");
            }
            else
            {
                psi.ArgumentList.Add("-w");
                psi.ArgumentList.Add(outputPath);
            }
        }

        psi.ArgumentList.Add(text);

        Process proc;
        try
        {
            proc = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start '{psi.FileName}'.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                $"'{psi.FileName}' not found. Install it ({(_mac ? "built into macOS" : "e.g. `apt install espeak`")}).");
        }

        using (proc)
        {
            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException($"{psi.FileName} exited with code {proc.ExitCode}.");
            }
        }
    }

    public IReadOnlyList<string> ListVoices()
    {
        var psi = new ProcessStartInfo
        {
            FileName = _mac ? "say" : "espeak",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        if (_mac) { psi.ArgumentList.Add("-v"); psi.ArgumentList.Add("?"); }
        else { psi.ArgumentList.Add("--voices"); }

        try
        {
            using var proc = Process.Start(psi);
            if (proc is null) return [];
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch
        {
            return [];
        }
    }
}
