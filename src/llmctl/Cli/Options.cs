namespace llmctl.Cli;

internal record Options
{
    // Backend override. When Endpoint is null, llmctl resolves through the registry
    // (best available: local Ollama → offline model → cloud). Set --endpoint (or the
    // env var) to talk to one specific OpenAI-compatible endpoint directly.
    public string? Endpoint { get; init; } = Environment.GetEnvironmentVariable("IDLEOPS_LLM_ENDPOINT");
    public string? Model { get; init; } = Environment.GetEnvironmentVariable("IDLEOPS_LLM_MODEL");
    public string? ApiKey { get; init; } = Environment.GetEnvironmentVariable("IDLEOPS_LLM_API_KEY");

    // Prompt
    public string? Goal { get; init; }       // the user/task prompt
    public string? Image { get; init; }      // optional screenshot to reason over (vision models)
    public string? System { get; init; }     // optional system-prompt override (e.g. "reply as JSON {action,reason}")
    public double Temperature { get; init; } = 0.2;

    public bool List { get; init; }          // print the resolved registry + which backend is active
    public bool ShowHelp { get; init; }

    public bool HasAction => Goal is not null || List;
}
