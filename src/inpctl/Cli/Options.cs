namespace inpctl.Cli;

/// <summary>
/// How held/sustained input is delivered. The right method depends on how the
/// target consumes input — hence a qualifier rather than a single fixed approach.
/// </summary>
internal enum InputMethod
{
    /// <summary>Inject via SendInput to the focused window (requires the target to be focused).</summary>
    Foreground,
    /// <summary>Post messages to the target window without stealing focus (works only on targets that process their window message queue).</summary>
    Background,
}

internal record Options
{
    public string? Window { get; init; }
    public string? Keyboard { get; init; }
    public string? Type { get; init; }
    public string? LeftMouse { get; init; }
    public string? RightMouse { get; init; }
    public string? MiddleMouse { get; init; }
    public int? Pid { get; init; }
    public bool SendCtrlC { get; init; }
    public bool MoveCursor { get; init; }
    public bool Background { get; init; }
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
    public string? Resize { get; init; }
    public string? Move { get; init; }
    public bool Maximize { get; init; }
    public bool Minimize { get; init; }
    public bool Restore { get; init; }

    // Hold / sustained input
    public string? Hold { get; init; }               // key(s) to hold, e.g. "F" or "W,SHIFT"
    public int Interval { get; init; } = 30;         // ms between re-posts (Background method)
    public double Duration { get; init; }            // seconds to hold; 0 = until Ctrl+C
    public InputMethod Method { get; init; } = InputMethod.Foreground;

    public bool HasAction =>
        SendCtrlC || Keyboard is not null || Type is not null
        || LeftMouse is not null || RightMouse is not null || MiddleMouse is not null
        || Resize is not null || Move is not null || Maximize || Minimize || Restore
        || Hold is not null;
}
