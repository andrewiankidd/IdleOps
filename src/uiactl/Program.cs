using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using IdleOps.Shared.Logging;
using IdleOps.Shared.Platform;
using IdleOps.Shared.Windows.Uia;
using uiactl.Cli;

namespace uiactl;

internal static class Program
{
    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = OptionsParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            ConsoleLogger.Error($"[uiactl] {ex.Message}");
            return 1;
        }

        if (options.ShowHelp || !options.HasVerb)
        {
            HelpFactory.PrintHelp();
            return options.HasVerb ? 0 : 1;
        }

        if (!PlatformSupport.EnsureWindows("uiactl")) return 1;

        return Run(options);
    }

    [SupportedOSPlatform("windows")]
    private static int Run(Options options)
    {
        UiaAutomation uia;
        try
        {
            uia = new UiaAutomation();
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"[uiactl] failed to start UI Automation: {ex.Message}");
            return 1;
        }

        // Screen-point query needs no window.
        if (options.ElementAt is not null)
        {
            if (!TryParsePoint(options.ElementAt, out var x, out var y))
            {
                ConsoleLogger.Error("[uiactl] --element-at expects \"x,y\".");
                return 1;
            }
            var info = uia.ElementAt(x, y);
            if (info is null) { ConsoleLogger.Warn("[uiactl] no element at that point."); return 1; }
            Console.WriteLine(Format(info));
            return 0;
        }

        if (string.IsNullOrWhiteSpace(options.Window))
        {
            ConsoleLogger.Error("[uiactl] --window is required.");
            return 1;
        }

        if (options.Dump)
        {
            var dump = uia.Dump(options.Window!, options.Max);
            if (dump is null) { ConsoleLogger.Warn($"[uiactl] window '{options.Window}' not found."); return 1; }
            ConsoleLogger.Info($"uiactl: {dump.Total} elements (showing {dump.Shown.Count})");
            foreach (var el in dump.Shown) Console.WriteLine(Format(el));
            if (dump.Total > options.Max) ConsoleLogger.Info($"uiactl: {dump.Total - options.Max} more not shown (raise --max).");
            return 0;
        }

        int? controlType = null;
        if (!string.IsNullOrEmpty(options.ControlType))
        {
            controlType = ControlTypes.Parse(options.ControlType);
            if (controlType is null) { ConsoleLogger.Error($"[uiactl] unknown --control-type '{options.ControlType}'."); return 1; }
        }
        var selector = new Selector(options.AutomationId, options.Name, controlType);
        if (selector.IsEmpty)
        {
            ConsoleLogger.Error("[uiactl] a selector (--automation-id / --name / --control-type) is required.");
            return 1;
        }

        var result = Dispatch(uia, options, selector);
        if (result.Value is not null) Console.WriteLine(result.Value);
        else if (result.Ok) ConsoleLogger.Info($"[uiactl] {result.Message}");
        else ConsoleLogger.Warn($"[uiactl] {result.Message}");
        return result.Ok ? 0 : 1;
    }

    [SupportedOSPlatform("windows")]
    private static UiaResult Dispatch(UiaAutomation uia, Options o, Selector sel)
    {
        var w = o.Window!;
        if (o.SetValue is not null) return uia.SetValue(w, sel, o.SetValue);
        if (o.GetValue) return uia.GetValue(w, sel);
        if (o.Invoke) return uia.Invoke(w, sel);
        if (o.Toggle) return uia.Toggle(w, sel);
        if (o.Expand) return uia.ExpandCollapse(w, sel, expand: true);
        if (o.Collapse) return uia.ExpandCollapse(w, sel, expand: false);
        if (o.Select) return uia.Select(w, sel);
        return UiaResult.Fail("no verb to dispatch.");
    }

    private static string Format(ElementInfo el) =>
        $"[{el.ControlType}] name=\"{el.Name}\" automation-id=\"{el.AutomationId}\" patterns=[{string.Join(", ", el.Patterns)}]";

    private static bool TryParsePoint(string token, out int x, out int y)
    {
        x = y = 0;
        var parts = token.Split(',', 2);
        return parts.Length == 2 && int.TryParse(parts[0].Trim(), out x) && int.TryParse(parts[1].Trim(), out y);
    }
}
