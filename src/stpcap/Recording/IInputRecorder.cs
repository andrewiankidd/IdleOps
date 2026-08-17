namespace stpcap.Recording;

/// <summary>
/// Captures global input into <see cref="InputEvent"/>s until stopped. Windows uses
/// low-level hooks (WH_*_LL); Linux uses XRecord via a python-xlib helper. Selected
/// by <see cref="InputRecorderFactory"/> so Program stays platform-agnostic.
/// </summary>
internal interface IInputRecorder : IDisposable
{
    string Name { get; }

    /// <summary>Capture until <paramref name="token"/> is cancelled (blocks the calling thread).</summary>
    void RunUntil(CancellationToken token);

    IReadOnlyList<InputEvent> Events { get; }
}

/// <summary>Picks the input recorder for the current OS. Null on an unsupported platform.</summary>
internal static class InputRecorderFactory
{
    public static IInputRecorder? Create(string? windowFilter)
    {
        if (OperatingSystem.IsWindows()) return new WindowsInputRecorder(windowFilter);
        if (OperatingSystem.IsLinux()) return new LinuxInputRecorder(windowFilter);
        return null;
    }
}
