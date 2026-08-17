namespace llmctl.Llm;

/// <summary>
/// Curated AI backends (ported from POSEIDON's poseidon-ai presets): hosted
/// OpenAI-compatible providers (bring an API key) and small offline GGUF models
/// that are downloaded and run in-process. Kept as data so the registry can
/// validate against them and a UI could render the choices.
/// </summary>
internal sealed record OnlineProvider(string Id, string Label, string Endpoint, string DefaultModel, string KeyUrl);

internal sealed record OfflineModel(string Id, string Label, string Repo, string File, string TokenizerRepo, int SizeMb);

internal static class Presets
{
    // All three expose an OpenAI-compatible /chat/completions API, so one client covers them.
    public static readonly IReadOnlyList<OnlineProvider> OnlineProviders =
    [
        new("anthropic", "Claude (Anthropic)", "https://api.anthropic.com/v1/chat/completions", "claude-3-5-haiku-latest", "https://console.anthropic.com/settings/keys"),
        new("openai", "ChatGPT (OpenAI)", "https://api.openai.com/v1/chat/completions", "gpt-4o-mini", "https://platform.openai.com/api-keys"),
        new("gemini", "Gemini (Google)", "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-1.5-flash", "https://aistudio.google.com/apikey"),
    ];

    // Qwen2.5 GGUF (text). Vision self-heal needs an online VL model or an Ollama VL
    // model; these small offline models cover text-only tasks with zero setup.
    public static readonly IReadOnlyList<OfflineModel> OfflineModels =
    [
        new("qwen2.5-0.5b", "Qwen2.5 0.5B — fastest, lightest (~400 MB)", "Qwen/Qwen2.5-0.5B-Instruct-GGUF", "qwen2.5-0.5b-instruct-q4_k_m.gguf", "Qwen/Qwen2.5-0.5B-Instruct", 400),
        new("qwen2.5-1.5b", "Qwen2.5 1.5B — balanced (~1 GB)", "Qwen/Qwen2.5-1.5B-Instruct-GGUF", "qwen2.5-1.5b-instruct-q4_k_m.gguf", "Qwen/Qwen2.5-1.5B-Instruct", 1000),
        new("qwen2.5-3b", "Qwen2.5 3B — more accurate (~2 GB; GPU)", "Qwen/Qwen2.5-3B-Instruct-GGUF", "qwen2.5-3b-instruct-q4_k_m.gguf", "Qwen/Qwen2.5-3B-Instruct", 2000),
        // The official Qwen 7B GGUF ships q4_k_m as a 2-part split, which the
        // single-file loader can't use; bartowski publishes an identical-quant
        // single file, so use that.
        new("qwen2.5-7b", "Qwen2.5 7B — best accuracy (~4.7 GB; needs a GPU)", "bartowski/Qwen2.5-7B-Instruct-GGUF", "Qwen2.5-7B-Instruct-Q4_K_M.gguf", "Qwen/Qwen2.5-7B-Instruct", 4700),
    ];

    public static OnlineProvider? OnlineProviderById(string id) =>
        OnlineProviders.FirstOrDefault(p => p.Id == id);

    public static OfflineModel? OfflineModelById(string id) =>
        OfflineModels.FirstOrDefault(m => m.Id == id);
}
