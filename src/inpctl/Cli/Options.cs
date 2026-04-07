namespace inpctl.Cli;

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
    public bool ShowHelp { get; init; }
    public string? Resize { get; init; }
    public string? Move { get; init; }
    public bool Maximize { get; init; }
    public bool Minimize { get; init; }
    public bool Restore { get; init; }

    public bool HasAction =>
        SendCtrlC || Keyboard is not null || Type is not null
        || LeftMouse is not null || RightMouse is not null || MiddleMouse is not null
        || Resize is not null || Move is not null || Maximize || Minimize || Restore;
}
