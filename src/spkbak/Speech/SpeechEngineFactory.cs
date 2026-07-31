namespace spkbak.Speech;

internal static class SpeechEngineFactory
{
    public static ISpeechEngine Create() =>
#if WINDOWS
        new WindowsSpeechEngine();
#else
        new UnixSpeechEngine();
#endif
}
