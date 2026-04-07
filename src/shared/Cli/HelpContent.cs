namespace IdleOps.Shared.Cli;

public record HelpContent(
    string Name,
    string Description,
    IReadOnlyList<string> UsageExamples,
    IReadOnlyList<string> Options);
