using System.Reflection;

namespace IdleOps.Shared.Cli;

/// <summary>
/// Identifies which build of a tool is running.
///
/// AssemblyVersion is a constant 1.0.0.0 across every tool and every commit, so it cannot
/// answer "which build is this?" — and with a rolling `latest-main` release that is rebuilt
/// on every push, that question is the first one any bug report needs. The commit is
/// stamped in at build time via SourceRevisionId (see src/Directory.Build.props), which the
/// SDK folds into AssemblyInformationalVersion as "1.0.0+&lt;sha&gt;"; this reads it back.
/// </summary>
public static class BuildInfo
{
    /// <summary>Short commit the binary was built from, or null for a build made without git.</summary>
    public static string? Commit { get; } = ReadCommit();

    /// <summary>Product version without the commit suffix, e.g. "1.0.0".</summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>
    /// The one-line banner every tool leads its help with, e.g.
    /// <c>IdleOps - waitfr (fc63580)</c>. The commit is omitted when unknown rather than
    /// printing a placeholder that looks like a real revision.
    /// </summary>
    public static string Banner(string toolName) =>
        Commit is { Length: > 0 } c ? $"IdleOps - {toolName} ({c})" : $"IdleOps - {toolName}";

    /// <summary>Fuller form for --version, e.g. "1.0.0 (fc63580)".</summary>
    public static string VersionLine() =>
        Commit is { Length: > 0 } c ? $"{Version} ({c})" : Version;

    private static string? ReadCommit()
    {
        var informational = Informational();
        if (informational is null) return null;
        // "1.0.0+fc63580" — the SDK's own separator for the source revision.
        var plus = informational.IndexOf('+');
        return plus >= 0 && plus < informational.Length - 1 ? informational[(plus + 1)..] : null;
    }

    private static string ReadVersion()
    {
        var informational = Informational();
        if (informational is null) return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }

    // The entry assembly is the tool itself; shared.dll's own attribute would be wrong here.
    private static string? Informational() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
