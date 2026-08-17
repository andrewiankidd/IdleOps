using System.Runtime.Versioning;
using System.Threading;
using inpctl.Input;
using Xunit;

namespace inpctl.Tests;

// End-to-end: post input to a real (message-only) window and assert its WndProc
// received it with the right codes. Needs a Windows desktop/message pump, so these
// are excluded from CI by the "IntegrationTests" name filter (build.yml).
//
// [WindowsOnlyFact] rather than [Fact] so they also self-skip off Windows: the CI
// filter is the only thing that was hiding them, and a plain `dotnet test` on
// Linux/macOS failed all three on user32 P/Invoke.
[SupportedOSPlatform("windows")]
public class InputDeliveryIntegrationTests
{
    private const int VkF = 0x46;

    [WindowsOnlyFact]
    public void BackgroundKeyboard_PostsCorrectVirtualKey()
    {
        using var win = new MessageWindow();

        Assert.True(InputSender.SendKeyboard("F", win.Handle, background: true));
        Thread.Sleep(150);  // let the window pump process the posted messages

        // Regression guard: must be the F vk (0x46), not VkKeyScan's shifted 0x146.
        Assert.True(win.Count(MessageWindow.WM_KEYDOWN, VkF) >= 1, "expected WM_KEYDOWN for F (0x46)");
        Assert.True(win.Count(MessageWindow.WM_KEYUP, VkF) >= 1, "expected WM_KEYUP for F (0x46)");
        Assert.Equal(0, win.Count(MessageWindow.WM_KEYDOWN, 0x146));
    }

    [WindowsOnlyFact]
    public void BackgroundHold_RepostsKeyDown_ThenReleases()
    {
        using var win = new MessageWindow();

        // Hold F for 0.3s at 20ms interval -> roughly 15 re-posts.
        Assert.True(InputSender.HoldBackground("F", win.Handle, intervalMs: 20, durationSeconds: 0.3, CancellationToken.None));
        Thread.Sleep(150);

        Assert.True(win.Count(MessageWindow.WM_KEYDOWN, VkF) >= 5, "hold should re-post several WM_KEYDOWN for F");
        Assert.True(win.Count(MessageWindow.WM_KEYUP, VkF) >= 1, "hold should post WM_KEYUP on release");
    }

    [WindowsOnlyFact]
    public void BackgroundType_PostsCharacters()
    {
        using var win = new MessageWindow();

        Assert.True(InputSender.TypeText("hi", win.Handle, background: true));
        Thread.Sleep(150);

        Assert.True(win.Count(MessageWindow.WM_CHAR, 'h') >= 1);
        Assert.True(win.Count(MessageWindow.WM_CHAR, 'i') >= 1);
    }
}
