using outcap.Cli;
using Xunit;

namespace outcap.Tests;

public class OptionsParserTests
{
    [Fact]
    public void DefaultsAreApplied()
    {
        var options = OptionsParser.Parse(Array.Empty<string>());

        Assert.False(options.ShowHelp);
        Assert.False(options.ShowVersion);
        Assert.False(options.KeepRaw);
        Assert.Null(options.TimerSeconds);
        Assert.Null(options.DelaySeconds);

        Assert.Equal(Path.GetFullPath("outcap.mp4"), options.MergedOutput);
        Assert.Equal(Path.GetFullPath("outcap-video.mp4"), options.Video.OutputPath);
        Assert.Equal(Path.GetFullPath("outcap-audio.wav"), options.Audio.OutputPath);
        Assert.Null(options.Video.WindowTitle);
        Assert.Null(options.Video.DelaySeconds);
        Assert.Null(options.Video.TimerSeconds);
        Assert.Null(options.Audio.DelaySeconds);
        Assert.Null(options.Audio.TimerSeconds);
    }

    [Fact]
    public void GlobalTimerAndDelayPropagateWhenNotOverridden()
    {
        var options = OptionsParser.Parse(new[]
        {
            "-t", "12",
            "-d", "1.5",
            "--output", "merged.mp4",
            "--vid-output", "v.mp4",
            "--aud-output", "a.wav"
        });

        Assert.Equal(12, options.TimerSeconds);
        Assert.Equal(1.5, options.DelaySeconds);

        Assert.Equal(12, options.Video.TimerSeconds);
        Assert.Equal(1.5, options.Video.DelaySeconds);
        Assert.Equal(12, options.Audio.TimerSeconds);
        Assert.Equal(1.5, options.Audio.DelaySeconds);

        Assert.Equal(Path.GetFullPath("merged.mp4"), options.MergedOutput);
        Assert.Equal(Path.GetFullPath("v.mp4"), options.Video.OutputPath);
        Assert.Equal(Path.GetFullPath("a.wav"), options.Audio.OutputPath);
    }

    [Fact]
    public void PerStreamOverridesTakePrecedence()
    {
        var options = OptionsParser.Parse(new[]
        {
            "-t", "20",
            "-d", "5",
            "--vid-timer", "8",
            "--vid-delay", "2",
            "--aud-timer", "10",
            "--aud-delay", "3",
            "--vid-window", "My Window"
        });

        Assert.Equal(20, options.TimerSeconds);
        Assert.Equal(5, options.DelaySeconds);

        Assert.Equal(8, options.Video.TimerSeconds);
        Assert.Equal(2, options.Video.DelaySeconds);
        Assert.Equal("My Window", options.Video.WindowTitle);

        Assert.Equal(10, options.Audio.TimerSeconds);
        Assert.Equal(3, options.Audio.DelaySeconds);
    }
}
