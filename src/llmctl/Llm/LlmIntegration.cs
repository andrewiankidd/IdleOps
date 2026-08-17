using System.Text.Json.Serialization;

namespace llmctl.Llm;

/// <summary>
/// One AI integration in the ordered registry (ported from POSEIDON's
/// LlmIntegration). kind = "online" (HTTP: cloud or local endpoint) or "offline"
/// (embedded GGUF). compatible() gates on platform; configured() gates on being
/// filled in enough to run; build() produces the callable backend.
/// </summary>
internal sealed class LlmIntegration
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";           // online | offline
    public string? Provider { get; set; }            // online: anthropic|openai|gemini|custom
    public string? Endpoint { get; set; }            // online: overrides the provider preset
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
    public string? OfflineModel { get; set; }         // offline: a Presets.OfflineModels id
    public string Device { get; set; } = "cpu";       // offline: gpu|cpu

    /// <summary>Could this integration handle an image task? Offline GGUF is text-only.</summary>
    [JsonIgnore]
    public bool MightSupportVision => Kind == "online";

    public bool Compatible(PlatformCaps caps) => Kind switch
    {
        "online" => true,                                            // any network runs an HTTP endpoint
        "offline" => caps.Embedded && (Device != "gpu" || caps.Gpu),
        _ => false,
    };

    /// <summary>Filled in enough to run (not whether it will succeed at runtime).</summary>
    public bool Configured() => Kind switch
    {
        // A hosted preset needs a key; a custom endpoint (Ollama/LM Studio) does not.
        "online" => ResolveEndpoint() is not null && (IsCustom() || !string.IsNullOrWhiteSpace(EffectiveApiKey())),
        "offline" => !string.IsNullOrWhiteSpace(OfflineModel) && Presets.OfflineModelById(OfflineModel!) is not null,
        _ => false,
    };

    public IChatBackend? Build(double temperature)
    {
        switch (Kind)
        {
            case "online":
                var endpoint = ResolveEndpoint();
                if (endpoint is null) return null;
                return new OnlineBackend(endpoint, ResolveModel(), EffectiveApiKey(), temperature,
                    $"online:{Provider ?? "custom"}:{ResolveModel()}");
            case "offline":
                return OfflineModel is null ? null : EmbeddedBackend.TryCreate(OfflineModel, Device, (float)temperature);
            default:
                return null;
        }
    }

    /// <summary>
    /// The API key, resolved from the config value if present, else from the
    /// environment — so keys stay out of the plaintext config file.
    /// </summary>
    public string? EffectiveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey)) return ApiKey;
        var envName = Provider switch
        {
            "anthropic" => "ANTHROPIC_API_KEY",
            "openai" => "OPENAI_API_KEY",
            "gemini" => "GEMINI_API_KEY",
            _ => null,
        };
        var byProvider = envName is not null ? Environment.GetEnvironmentVariable(envName) : null;
        return !string.IsNullOrWhiteSpace(byProvider) ? byProvider : Environment.GetEnvironmentVariable("IDLEOPS_LLM_API_KEY");
    }

    private bool IsCustom() => string.IsNullOrEmpty(Provider) || Provider == "custom";

    private string? ResolveEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(Endpoint)) return Endpoint;
        if (Provider is not null && Presets.OnlineProviderById(Provider) is { } p) return p.Endpoint;
        return null;
    }

    private string ResolveModel()
    {
        if (!string.IsNullOrWhiteSpace(Model)) return Model;
        if (Provider is not null && Presets.OnlineProviderById(Provider) is { } p) return p.DefaultModel;
        return "";
    }
}
