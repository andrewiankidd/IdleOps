using System.Globalization;
using System.Runtime.Versioning;
using IdleOps.Shared.Capture;

namespace IdleOps.Shared.Windowing;

/// <summary>Linux (X11) window presence via the shared <see cref="LinuxX11Windows"/> search.</summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxWindowLocator : IWindowLocator
{
    public string Name => "xdotool (X11)";

    public bool Exists(string pattern) => LinuxX11Windows.SearchId(pattern) is not null;

    public nint Resolve(string pattern) =>
        long.TryParse(LinuxX11Windows.SearchId(pattern), out var id) ? (nint)id : 0;

    public string? ResolveTitle(string pattern)
    {
        var id = LinuxX11Windows.SearchId(pattern);
        if (id is null) return null;
        var (ok, stdout, _) = ProcessRunner.Run("xdotool", "getwindowname", id);
        return ok ? stdout.Trim() : null;
    }

    public WindowBounds? GetBounds(string pattern)
    {
        var id = LinuxX11Windows.SearchId(pattern);
        if (id is null) return null;
        var (ok, stdout, _) = ProcessRunner.Run("xdotool", "getwindowgeometry", "--shell", id);
        if (!ok) return null;
        int x = 0, y = 0, w = 0, h = 0;
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = line.IndexOf('=');
            if (eq <= 0 || !int.TryParse(line[(eq + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) continue;
            switch (line[..eq]) { case "X": x = v; break; case "Y": y = v; break; case "WIDTH": w = v; break; case "HEIGHT": h = v; break; }
        }
        return new WindowBounds(x, y, w, h);
    }
}
