namespace inpctl.Cli;

internal static class HelpFactory
{
    public static void PrintHelp()
    {
        Console.WriteLine("""
            Usage: inpctl [--window "<title>"] [--keyboard "<keys>"] [--type "<text>"] [--leftmouse "x,y"]

            Input:
              --keyboard <keys>     Send key sequence (comma-separated, e.g., CTRL+C,ALT+TAB)
              --type <text>         Type literal text (handles shifted characters)
              --leftmouse <coords>  Left-click at coordinates
              --rightmouse <coords> Right-click at coordinates
              --middlemouse <coords> Middle-click at coordinates
              --move-cursor         Physically move the mouse cursor
              --background          Post --type/--keyboard without foregrounding the window
                                    (no focus steal; classic Win32 only, not webviews)

            Window:
              -w, --window <title>  Target window by title (supports * wildcards)
              --resize <WxH>        Resize window (e.g., 1280x720)
              --move <x,y>          Move window to position
              --maximize            Maximize window
              --minimize            Minimize window
              --restore             Restore window

            Process:
              --pid <pid>           Target process ID (for --ctrlc)
              --ctrlc               Send Ctrl+C signal to process

            Other:
              -h, --help            Show help

            Examples:
              inpctl --window "Notepad*" --type "hello world!"
              inpctl --window "Notepad*" --type "typed without focus" --background
              inpctl --window "Notepad*" --keyboard "CTRL+S"
              inpctl --window "Paint*" --leftmouse "50%,50%"
              inpctl --window "My App*" --resize 1280x720 --move 0,0
              inpctl --pid 12345 --ctrlc
            """);
    }
}
