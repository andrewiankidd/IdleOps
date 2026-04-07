using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace IdleOps.Shared.Windows;

/// <summary>
/// Captures a screenshot of a window as a System.Drawing.Bitmap.
/// Uses PrintWindow (works for partially occluded windows) with BitBlt fallback.
/// Windows-only.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowCapture
{
    /// <summary>
    /// Capture a window screenshot. Returns a Bitmap in 32bppArgb format.
    /// </summary>
    public static Bitmap CaptureWindow(IntPtr handle)
    {
        var rect = WindowMatcher.GetWindowBounds(handle);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            throw new InvalidOperationException("Window has invalid bounds (possibly minimized).");
        }

        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();

        try
        {
            // PrintWindow with PW_RENDERFULLCONTENT captures DPI-aware content
            if (!NativeMethods.PrintWindow(handle, hdc, NativeMethods.PW_RENDERFULLCONTENT))
            {
                // Fallback to BitBlt from screen DC
                var screenDc = NativeMethods.GetDC(IntPtr.Zero);
                try
                {
                    NativeMethods.BitBlt(hdc, 0, 0, rect.Width, rect.Height,
                        screenDc, rect.Left, rect.Top, NativeMethods.SRCCOPY);
                }
                finally
                {
                    NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        return bitmap;
    }
}
