namespace llmctl.Llm;

/// <summary>Outcome of a registry completion: the reply, which backend served it, and per-backend errors on the way.</summary>
public sealed record LlmResult(bool Ok, string Text, string? Backend, IReadOnlyList<string> Errors);

/// <summary>
/// The public entry point other IdleOps tools (e.g. playbk) use to get an AI
/// completion without touching the registry internals. It loads the autotuned /
/// persisted registry, walks the active backends best-first, and returns the first
/// success — so keys stay in the environment, offline models auto-download, and an
/// image task skips text-only backends automatically.
/// </summary>
public static class LlmRunner
{
    /// <summary>
    /// Run one completion through the resolved registry. When <paramref name="imageBase64"/>
    /// is set, only vision-capable backends are tried. <paramref name="onAttempt"/> fires
    /// with each backend's label just before it runs (for "using X" progress on slow
    /// backends). Returns Ok=false with per-backend errors if nothing succeeds.
    /// </summary>
    public static async Task<LlmResult> CompleteAsync(
        string? system,
        string goal,
        string? imageBase64,
        double temperature,
        CancellationToken token,
        Action<string>? onAttempt = null)
    {
        var caps = PlatformCaps.Detect();
        var candidates = LlmConfig.Load().ResolveAll(caps).ToList();
        if (candidates.Count == 0)
        {
            return new LlmResult(false, "", null,
                ["no AI backend available (run `llmctl --list`, start Ollama, or set an API key)"]);
        }

        var errors = new List<string>();
        foreach (var integration in candidates)
        {
            // Skip text-only backends for image tasks BEFORE building (an offline
            // Build downloads a model — don't fetch one we can't use).
            if (imageBase64 is not null && !integration.MightSupportVision) continue;

            var backend = integration.Build(temperature);
            if (backend is null) continue;                                   // e.g. offline load/download failed
            if (imageBase64 is not null && !backend.SupportsVision) continue; // belt-and-suspenders

            try
            {
                onAttempt?.Invoke(backend.Describe());
                var reply = await backend.CompleteAsync(system, goal, imageBase64, token);
                return new LlmResult(true, reply, backend.Describe(), errors);
            }
            catch (Exception ex)
            {
                errors.Add($"{integration.Id}: {ex.Message}");
            }
        }

        return new LlmResult(false, "", null, errors);
    }
}
