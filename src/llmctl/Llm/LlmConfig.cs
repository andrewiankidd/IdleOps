using System.Text.Json;
using System.Text.Json.Serialization;

namespace llmctl.Llm;

/// <summary>
/// An ordered registry of AI integrations (order = priority), ported from
/// POSEIDEN's LlmConfig. The first entry that is compatible with the platform AND
/// configured becomes active; callers can walk the ordered candidates and fall
/// through on runtime failure, so you always get whatever AI is actually available.
/// </summary>
internal sealed class LlmConfig
{
    public List<LlmIntegration> Integrations { get; set; } = [];
    public bool Auto { get; set; }

    /// <summary>Active candidates (compatible + configured), best-first.</summary>
    public IEnumerable<LlmIntegration> ResolveAll(PlatformCaps caps)
        => Integrations.Where(i => i.Compatible(caps) && i.Configured());

    public LlmIntegration? Resolve(PlatformCaps caps) => ResolveAll(caps).FirstOrDefault();

    /// <summary>
    /// The default catalog, ordered by real throughput: on-device GPU first, then
    /// on-device CPU, then a local Ollama endpoint (the vision path), then cloud
    /// templates (keyless until a key is present in the environment). Offline model
    /// ids here are placeholders — <see cref="Autotuned"/> sizes them to the machine.
    /// </summary>
    public static LlmConfig Seeded() => new()
    {
        Integrations =
        [
            new LlmIntegration { Id = "local-gpu", Name = "On-device GPU", Kind = "offline", OfflineModel = "qwen2.5-1.5b", Device = "gpu" },
            new LlmIntegration { Id = "local-cpu", Name = "On-device CPU", Kind = "offline", OfflineModel = "qwen2.5-0.5b", Device = "cpu" },
            new LlmIntegration { Id = "ollama", Name = "Local Ollama", Kind = "online", Provider = "custom", Endpoint = "http://localhost:11434/v1", Model = "qwen2.5vl:7b" },
            new LlmIntegration { Id = "anthropic", Name = "Claude (Anthropic)", Kind = "online", Provider = "anthropic" },
            new LlmIntegration { Id = "gemini", Name = "Gemini (Google)", Kind = "online", Provider = "gemini" },
            new LlmIntegration { Id = "openai", Name = "ChatGPT (OpenAI)", Kind = "online", Provider = "openai" },
        ],
    };

    /// <summary>The seeded catalog with each offline entry's model sized to <paramref name="caps"/>.</summary>
    public static LlmConfig Autotuned(PlatformCaps caps)
    {
        var cfg = Seeded();
        foreach (var i in cfg.Integrations)
        {
            if (i.Kind == "offline")
            {
                i.OfflineModel = PlatformCaps.RecommendModel(i.Kind, i.Device, caps);
            }
        }
        cfg.Auto = true;
        return cfg;
    }

    public static LlmConfig Load()
    {
        var path = ConfigPath();
        if (File.Exists(path))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<LlmConfig>(File.ReadAllText(path), JsonOpts);
                if (cfg is { Integrations.Count: > 0 }) return cfg;
            }
            catch
            {
                // fall through to auto-config on a malformed file
            }
        }

        // First run: auto-configure for this machine and persist it, so setup is
        // transparent and the user has a file to reorder/edit.
        var auto = Autotuned(PlatformCaps.Detect());
        try { auto.Save(); } catch { /* read-only FS: still fully usable in-memory */ }
        return auto;
    }

    public void Save()
    {
        var path = ConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
    }

    public static string ConfigPath() =>
        Environment.GetEnvironmentVariable("IDLEOPS_LLM_CONFIG")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "idleops", "llm.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
