using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace stpcap.Recording;

/// <summary>
/// Raw interop for the X11 RECORD extension (libXtst) and the bits of libX11 needed to
/// turn a recorded event into something meaningful: keycode → keysym, and the active
/// window's title.
///
/// XRecord is a plain C API, so this is ordinary P/Invoke — no GObject, no D-Bus — which
/// is why the recorder no longer needs python3-xlib. Struct layouts below mirror
/// &lt;X11/extensions/record.h&gt; exactly; getting a field offset wrong here corrupts the
/// event stream rather than failing loudly, so they are spelled out with real field types
/// and let the runtime compute the same offsets the C compiler does.
/// </summary>
[SupportedOSPlatform("linux")]
internal static class XRecordInterop
{
    private const string X11 = "libX11.so.6";
    private const string XTst = "libXtst.so.6";

    /// <summary>Record from every client, present and future (XRecordAllClients).</summary>
    public const ulong AllClients = 3;

    /// <summary>The interception carried real protocol data (XRecordFromServer).</summary>
    public const int FromServer = 0;

    /// <summary>First callback after enabling a context (XRecordStartOfData); carries no events.</summary>
    public const int StartOfData = 4;

    // X core event types we ask for.
    public const byte KeyPress = 2;
    public const byte KeyRelease = 3;
    public const byte ButtonPress = 4;

    /// <summary>Every X core event is 32 bytes on the wire.</summary>
    public const int EventSize = 32;

    // --- record.h layouts -----------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public struct XRecordRange8 { public byte First; public byte Last; }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRecordRange16 { public short First; public short Last; }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRecordExtRange { public XRecordRange8 ExtMajor; public XRecordRange16 ExtMinor; }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRecordRange
    {
        public XRecordRange8 CoreRequests;
        public XRecordRange8 CoreReplies;
        public XRecordExtRange ExtRequests;
        public XRecordExtRange ExtReplies;
        public XRecordRange8 DeliveredEvents;
        public XRecordRange8 DeviceEvents;
        public XRecordRange8 Errors;
        public int ClientStarted;   // Bool is int in Xlib
        public int ClientDied;
    }

    /// <summary>
    /// Field order matters and is easy to get wrong: several references list `category`
    /// first, but the real &lt;X11/extensions/record.h&gt; leads with the ids and puts `data`
    /// *before* `data_len`. Verified against a live server — the raw 48 bytes decode as
    /// id_base@0, server_time@8, client_seq@16, category@24, client_swapped@28, data@32,
    /// data_len@40. Getting this wrong yields a plausible-looking pointer and a garbage
    /// length rather than an error.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XRecordInterceptData
    {
        public UIntPtr IdBase;      // XID
        public UIntPtr ServerTime;  // Time
        public UIntPtr ClientSeq;
        public int Category;
        public int ClientSwapped;   // Bool
        public IntPtr Data;
        public uint DataLen;        // in 4-byte units
    }

    public delegate void XRecordInterceptProc(IntPtr closure, IntPtr recordedData);

    // --- libXtst --------------------------------------------------------------

    [DllImport(XTst)] public static extern IntPtr XRecordAllocRange();

    [DllImport(XTst)]
    public static extern IntPtr XRecordCreateContext(IntPtr display, int datumFlags,
        ulong[] clientSpecs, int nClients, IntPtr[] ranges, int nRanges);

    [DllImport(XTst)]
    public static extern int XRecordEnableContext(IntPtr display, IntPtr context,
        XRecordInterceptProc callback, IntPtr closure);

    [DllImport(XTst)] public static extern int XRecordDisableContext(IntPtr display, IntPtr context);
    [DllImport(XTst)] public static extern int XRecordFreeContext(IntPtr display, IntPtr context);
    [DllImport(XTst)] public static extern void XRecordFreeData(IntPtr data);

    // --- libX11 ---------------------------------------------------------------

    [DllImport(X11)] public static extern IntPtr XOpenDisplay(string? displayName);
    [DllImport(X11)] public static extern int XCloseDisplay(IntPtr display);
    [DllImport(X11)] public static extern int XFlush(IntPtr display);
    [DllImport(X11)] public static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport(X11)] public static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);
    [DllImport(X11)] public static extern int XFree(IntPtr data);
    [DllImport(X11)] public static extern int XGetInputFocus(IntPtr display, out IntPtr focusReturn, out int revertReturn);

    [DllImport(X11)]
    public static extern int XQueryTree(IntPtr display, IntPtr w, out IntPtr root, out IntPtr parent,
        out IntPtr children, out uint nChildren);

    [DllImport(X11)]
    public static extern int XGetWindowProperty(IntPtr display, IntPtr w, IntPtr property,
        long longOffset, long longLength, bool delete, IntPtr reqType,
        out IntPtr actualTypeReturn, out int actualFormatReturn,
        out ulong nItemsReturn, out ulong bytesAfterReturn, out IntPtr propReturn);

    /// <summary>Keycode → keysym for a given shift level (XKB, so it honours the layout).</summary>
    [DllImport(X11)] public static extern UIntPtr XkbKeycodeToKeysym(IntPtr display, byte keycode, int group, int level);

    /// <summary>Keysym → its X name ("Return", "Tab", ...), or NULL.</summary>
    [DllImport(X11)] public static extern IntPtr XKeysymToString(UIntPtr keysym);
}
