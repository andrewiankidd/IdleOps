using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace stpcap.Recording;

/// <summary>
/// Windows backend: the low-level-hook <see cref="InputRecorder"/> plus the message
/// pump the hooks require (moved here from Program). The hooks deliver callbacks via
/// this thread's message queue, so RunUntil must pump messages while it waits.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsInputRecorder : IInputRecorder
{
    private readonly InputRecorder _recorder;

    public WindowsInputRecorder(string? windowFilter) => _recorder = new InputRecorder(windowFilter);

    public string Name => "win32-hooks";
    public IReadOnlyList<InputEvent> Events => _recorder.Events;

    public void RunUntil(CancellationToken token)
    {
        _recorder.Start();
        try
        {
            while (!token.IsCancellationRequested)
            {
                while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                Thread.Sleep(10);
            }
        }
        finally
        {
            _recorder.Stop();
        }
    }

    public void Dispose() => _recorder.Dispose();

    private const uint PM_REMOVE = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr Hwnd; public uint Message; public IntPtr WParam; public IntPtr LParam;
        public uint Time; public int PtX; public int PtY;
    }

    [DllImport("user32.dll")] private static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref MSG msg);
}
