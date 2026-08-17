using IdleOps.Shared.Capture;
using Xunit;

namespace IdleOps.Shared.Tests;

/// <summary>
/// Parsing tests for the ffmpeg avfoundation device listing. The listing below is real
/// output captured from `ffmpeg -f avfoundation -list_devices true -i ""` on macOS 26,
/// including the noise lines ffmpeg emits around it. Parsing is pure, so this runs on
/// Windows and Linux CI too.
/// </summary>
public class AvFoundationDevicesTests
{
    private const string Listing = """
        [AVFoundation indev @ 0x7d5010140] AVFoundation video devices:
        [AVFoundation indev @ 0x7d5010140] [0] FaceTime HD Camera
        [AVFoundation indev @ 0x7d5010140] [1] Capture screen 0
        [AVFoundation indev @ 0x7d5010140] AVFoundation audio devices:
        [AVFoundation indev @ 0x7d5010140] [0] BlackHole 2ch
        [AVFoundation indev @ 0x7d5010140] [1] MacBook Pro Microphone
        [in#0 @ 0x7d5010000] Error opening input: Input/output error
        Error opening input file .
        """;

    [Fact]
    public void Parse_SplitsVideoAndAudioSections()
    {
        var (video, audio) = AvFoundationDevices.Parse(Listing);

        Assert.Equal(2, video.Count);
        Assert.Equal(2, audio.Count);
        Assert.Equal("FaceTime HD Camera", video[0].Name);
        Assert.Equal("Capture screen 0", video[1].Name);
        Assert.Equal("BlackHole 2ch", audio[0].Name);
    }

    // The screen is index 1 here only because a camera enumerates first — the whole point
    // of resolving by name rather than hardcoding an index.
    [Fact]
    public void Parse_KeepsTheIndexFfmpegReported()
    {
        var (video, audio) = AvFoundationDevices.Parse(Listing);

        Assert.Equal(1, video[1].Index);
        Assert.Equal(0, audio[0].Index);
    }

    [Fact]
    public void Parse_IgnoresNonDeviceLines()
    {
        var (video, audio) = AvFoundationDevices.Parse(Listing);

        Assert.DoesNotContain(video, d => d.Name.Contains("Error"));
        Assert.DoesNotContain(audio, d => d.Name.Contains("Error"));
    }

    [Fact]
    public void Parse_EmptyListing_YieldsNoDevices()
    {
        var (video, audio) = AvFoundationDevices.Parse("");

        Assert.Empty(video);
        Assert.Empty(audio);
    }
}
