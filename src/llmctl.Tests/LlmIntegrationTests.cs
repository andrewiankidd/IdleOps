using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using llmctl.Llm;
using Xunit;

namespace llmctl.Tests;

/// <summary>
/// Live tests that exercise the real AI stack on the host: caps detection, the
/// autotuned registry, an actual embedded GGUF completion (downloads a model on
/// first run), and a real Ollama round-trip when one is listening.
///
/// The "IntegrationTests" class-name suffix is what the CI filter excludes
/// (see .github/workflows/build.yml), so these never run in CI. Run them locally:
///   dotnet test src/llmctl.Tests --filter "FullyQualifiedName~IntegrationTests"
/// </summary>
public class LlmIntegrationTests
{
    // The smallest model — fastest to download and load for a smoke test.
    private const string SmokeModel = "qwen2.5-0.5b";

    [Fact]
    public void Autotune_ResolvesAtLeastOneBackendOnThisMachine()
    {
        var caps = PlatformCaps.Detect();
        var candidates = LlmConfig.Autotuned(caps).ResolveAll(caps);

        // Every embedded-capable host (Windows/Linux/macOS) has at least the
        // on-device CPU entry active, keyless — so AI is never zero.
        Assert.NotEmpty(candidates);
    }

    [Fact]
    public async Task Embedded_RealCompletion_ReturnsText()
    {
        // Force the tiny model regardless of caps so the download stays small.
        var backend = EmbeddedBackend.TryCreate(SmokeModel, device: "cpu", temperature: 0.0f);
        Assert.NotNull(backend);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // first run downloads ~400MB
        var reply = await backend!.CompleteAsync(
            system: "You are terse. Answer in a single word.",
            userText: "Name one primary color.",
            imageBase64: null,
            token: cts.Token);

        Assert.False(string.IsNullOrWhiteSpace(reply));
    }

    [Fact]
    public async Task Ollama_RealCompletion_WhenReachable()
    {
        const string endpoint = "http://localhost:11434/v1";
        if (!await IsReachableAsync("http://localhost:11434"))
        {
            // Soft-skip: Ollama isn't running on this host. (xunit 2.7 has no
            // Assert.Skip, so a reachable-guard keeps the suite green offline.)
            return;
        }

        var backend = new OnlineBackend(endpoint, "qwen2.5:0.5b", apiKey: null, temperature: 0.0, "ollama-live");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var reply = await backend.CompleteAsync(
            system: "You are terse.",
            userText: "Reply with the single word: pong",
            imageBase64: null,
            token: cts.Token);

        Assert.False(string.IsNullOrWhiteSpace(reply));
    }

    private static async Task<bool> IsReachableAsync(string url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            using var resp = await http.GetAsync(url);
            return true; // any HTTP response means the port is up
        }
        catch
        {
            return false;
        }
    }
}
