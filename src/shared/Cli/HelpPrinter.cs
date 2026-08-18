namespace IdleOps.Shared.Cli;

public static class HelpPrinter
{
    public static void Print(HelpContent content)
    {
        // Banner in place of the bare tool name: same line count, but it now says which
        // build produced this output — the thing a pasted --help was always missing.
        Console.WriteLine(BuildInfo.Banner(content.Name));
        Console.WriteLine(content.Description);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        foreach (var line in content.UsageExamples)
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine();
        Console.WriteLine("Options:");
        foreach (var option in content.Options)
        {
            Console.WriteLine($"  {option}");
        }
    }

    /// <summary>
    /// Prints the banner above a tool's own help text.
    ///
    /// Several tools have sectioned help (inpctl's Input/Hold/Window/Process, uiactl's
    /// Selector/Verbs) that <see cref="HelpContent"/>'s flat Usage+Options shape cannot
    /// express, so they keep their own body. This exists so they still share the one thing
    /// that actually needs to be consistent — the banner — instead of each re-implementing
    /// it, which is what let the two styles drift apart in the first place.
    /// </summary>
    public static void PrintRaw(string toolName, string body)
    {
        Console.WriteLine(BuildInfo.Banner(toolName));
        Console.WriteLine(body);
    }

    /// <summary>
    /// The caller's <paramref name="version"/> is ignored when the build carries a commit:
    /// every tool passes AssemblyVersion, which is a constant 1.0.0.0 and so identifies
    /// nothing. Kept in the signature so callers need no change.
    /// </summary>
    public static void PrintVersion(string name, string version)
    {
        Console.WriteLine(BuildInfo.Commit is null ? $"{name} {version}" : $"{name} {BuildInfo.VersionLine()}");
    }
}
