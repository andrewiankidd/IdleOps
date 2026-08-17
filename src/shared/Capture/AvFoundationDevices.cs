using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace IdleOps.Shared.Capture;

/// <summary>
/// Enumerates the ffmpeg avfoundation capture devices on this Mac.
///
/// avfoundation addresses inputs by *index*, and those indices are machine-specific:
/// cameras enumerate before screens, so the display is index 1 on a laptop with one
/// built-in camera but index 0 on a Mac mini with none, and audio index 0 is the
/// built-in microphone unless a loopback driver happens to sort first. Hardcoding an
/// index therefore records the wrong input — a webcam instead of the screen, a live
/// mic instead of system audio — with no error to show for it. Resolving the index by
/// device *name* at capture time is the only stable way to target them.
/// </summary>
public static class AvFoundationDevices
{
    /// <summary>Set to an explicit avfoundation index to bypass discovery.</summary>
    public const string VideoOverrideVariable = "IDLEOPS_AVFOUNDATION_VIDEO";

    /// <summary>Set to an explicit avfoundation index to bypass discovery.</summary>
    public const string AudioOverrideVariable = "IDLEOPS_AVFOUNDATION_AUDIO";

    // Loopback drivers, in preference order. macOS exposes no system-audio input of its
    // own, so capturing playback needs one of these installed and selected as output.
    private static readonly string[] LoopbackNames = ["BlackHole", "Loopback", "Soundflower", "Aggregate", "Multi-Output"];

    // "[AVFoundation indev @ 0x7f...] [1] Capture screen 0" -> (1, "Capture screen 0")
    private static readonly Regex DeviceLine = new(@"^\[AVFoundation indev @ [^\]]*\]\s*\[(\d+)\]\s*(.+?)\s*$", RegexOptions.Compiled);

    public readonly record struct Device(int Index, string Name);

    /// <summary>The avfoundation index of the screen-capture input.</summary>
    /// <exception cref="InvalidOperationException">No screen device is listed.</exception>
    [SupportedOSPlatform("macos")]
    public static string ResolveScreenIndex()
    {
        if (Override(VideoOverrideVariable) is { } forced) return forced;

        var (video, _) = List();
        var screen = video.FirstOrDefault(d => d.Name.Contains("Capture screen", StringComparison.OrdinalIgnoreCase));
        if (screen.Name is not null) return screen.Index.ToString();

        // The screen device is absent (not merely renumbered) when Screen Recording is
        // withheld, so the permission is the likeliest cause of an empty match.
        throw new InvalidOperationException(
            "ffmpeg lists no avfoundation screen-capture device" +
            (video.Count > 0 ? $" (it sees: {string.Join(", ", video.Select(d => $"[{d.Index}] {d.Name}"))})" : "") +
            $". This is usually a permissions issue: {Platform.MacPermissions.ScreenRecordingHint} " +
            $"Set {VideoOverrideVariable}=<index> to choose one explicitly.");
    }

    /// <summary>
    /// The avfoundation index to record system audio from: a loopback driver when one is
    /// installed, otherwise index 0 with a warning, since that is a live microphone.
    /// </summary>
    /// <exception cref="InvalidOperationException">No audio device is listed at all.</exception>
    [SupportedOSPlatform("macos")]
    public static string ResolveAudioIndex()
    {
        if (Override(AudioOverrideVariable) is { } forced) return forced;

        var (_, audio) = List();
        if (audio.Count == 0)
        {
            throw new InvalidOperationException(
                "ffmpeg lists no avfoundation audio devices. Check that ffmpeg has Microphone permission " +
                $"under System Settings > Privacy & Security, or set {AudioOverrideVariable}=<index>.");
        }

        foreach (var name in LoopbackNames)
        {
            var match = audio.FirstOrDefault(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (match.Name is not null)
            {
                Console.Error.WriteLine($"[audcap] Using loopback audio device [{match.Index}] {match.Name}.");
                return match.Index.ToString();
            }
        }

        Console.Error.WriteLine(
            $"[audcap] Warning: no loopback driver found, falling back to [{audio[0].Index}] {audio[0].Name} — " +
            "this records the microphone, not system audio. Install BlackHole (https://github.com/ExistentialAudio/BlackHole) " +
            $"and route playback through it, or set {AudioOverrideVariable}=<index>.");
        return audio[0].Index.ToString();
    }

    /// <summary>Video and audio devices as reported by ffmpeg, in listed order.</summary>
    [SupportedOSPlatform("macos")]
    public static (IReadOnlyList<Device> Video, IReadOnlyList<Device> Audio) List()
    {
        // This always "fails" — listing devices leaves ffmpeg with no input to open — so
        // the exit code says nothing and only the stderr listing matters.
        var (_, _, stderr) = ProcessRunner.Run("ffmpeg", "-hide_banner", "-f", "avfoundation", "-list_devices", "true", "-i", "");
        return Parse(stderr);
    }

    /// <summary>Splits an ffmpeg `-list_devices` dump into its video and audio sections.</summary>
    public static (IReadOnlyList<Device> Video, IReadOnlyList<Device> Audio) Parse(string listing)
    {
        List<Device> video = [], audio = [];
        List<Device>? current = null;
        foreach (var line in listing.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Contains("AVFoundation video devices:", StringComparison.Ordinal)) { current = video; continue; }
            if (line.Contains("AVFoundation audio devices:", StringComparison.Ordinal)) { current = audio; continue; }
            if (current is null) continue;

            var m = DeviceLine.Match(line);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var index)) current.Add(new Device(index, m.Groups[2].Value));
        }
        return (video, audio);
    }

    private static string? Override(string variable) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } v ? v.Trim() : null;
}
