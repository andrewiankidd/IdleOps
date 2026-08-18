namespace uiactl.Cli;

internal static class HelpFactory
{
    public static void PrintHelp()
    {
        IdleOps.Shared.Cli.HelpPrinter.PrintRaw("uiactl", """
            Usage: uiactl -w "<title>" [selector] <verb>

            Element-level desktop automation via UI Automation. Drives accessible
            controls (WinUI, WPF, UWP, WinForms, Win32) by their accessibility tree
            — focus-free, no OCR. Windows-only.

            Target:
              -w, --window <title>     Window to search (supports * wildcards)

            Selector (pick one; AutomationId wins, then Name, then ControlType):
              --automation-id <id>     Match element by AutomationId
              --name <name>            Match element by Name
              --control-type <type>    Match by control type (e.g. Edit, Button, or numeric id)

            Verbs:
              --set-value <text>       Set the element's value (ValuePattern)
              --get-value              Print the element's value to stdout
              --invoke                 Invoke the element (buttons, menu items)
              --toggle                 Toggle a checkbox / toggle button
              --expand                 Expand a menu / combo / tree item
              --collapse               Collapse a menu / combo / tree item
              --select                 Select a list item / tab / radio
              --dump                   List elements under the window (discover selectors)
              --max <n>                Max elements for --dump (default 60)
              --element-at <x,y>       Describe the element at a screen point (no --window needed)

            Other:
              -h, --help               Show help

            Examples:
              uiactl -w "*Notepad" --control-type Document --set-value "hello"
              uiactl -w "My App*" --automation-id "SaveButton" --invoke
              uiactl -w "My App*" --name "Enable telemetry" --toggle
              uiactl -w "Settings" --dump
            """);
    }
}
