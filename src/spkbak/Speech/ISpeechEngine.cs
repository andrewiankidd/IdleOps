namespace spkbak.Speech;

/// <summary>
/// Platform text-to-speech backend. Implementations: WinRT SpeechSynthesizer on
/// Windows, the `say` / `espeak` CLIs on macOS / Linux.
/// </summary>
internal interface ISpeechEngine
{
    /// <summary>Human-readable name of the backend (shown in status output).</summary>
    string Name { get; }

    /// <summary>Speak text aloud, or write it to <paramref name="outputPath"/> (WAV) if given.</summary>
    Task SpeakAsync(string text, string? voice, string? outputPath);

    /// <summary>Available voice names for this backend (best-effort).</summary>
    IReadOnlyList<string> ListVoices();
}
