namespace IdleOps.Shared.Windows.Uia;

/// <summary>
/// UIA control-type id ↔ name mapping. Pure (no COM / no OS calls) so it is unit
/// testable and reusable off the Windows-only UiaAutomation facade.
/// </summary>
public static class ControlTypes
{
    public static string Name(int id) => id switch
    {
        50000 => "Button", 50001 => "Calendar", 50002 => "CheckBox", 50003 => "ComboBox",
        50004 => "Edit", 50005 => "Hyperlink", 50006 => "Image", 50007 => "ListItem",
        50008 => "List", 50009 => "Menu", 50010 => "MenuBar", 50011 => "MenuItem",
        50012 => "ProgressBar", 50013 => "RadioButton", 50014 => "ScrollBar", 50015 => "Slider",
        50016 => "Spinner", 50017 => "StatusBar", 50018 => "Tab", 50019 => "TabItem",
        50020 => "Text", 50021 => "ToolBar", 50022 => "ToolTip", 50023 => "Tree",
        50024 => "TreeItem", 50025 => "Custom", 50026 => "Group", 50027 => "Thumb",
        50028 => "DataGrid", 50029 => "DataItem", 50030 => "Document", 50031 => "SplitButton",
        50032 => "Window", 50033 => "Pane", 50034 => "Header", 50035 => "HeaderItem",
        50036 => "Table", 50037 => "TitleBar", 50038 => "Separator",
        _ => id == 0 ? "?" : id.ToString(),
    };

    /// <summary>Map a control-type name (e.g. "Edit") or numeric id to its UIA id.</summary>
    public static int? Parse(string token)
    {
        if (int.TryParse(token, out var numeric)) return numeric;
        for (var id = 50000; id <= 50038; id++)
        {
            if (string.Equals(Name(id), token, StringComparison.OrdinalIgnoreCase)) return id;
        }
        return null;
    }
}
