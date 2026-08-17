using System.Linq;
using llmctl.Llm;
using Xunit;

namespace llmctl.Tests;

public class RegistryTests
{
    private static readonly PlatformCaps Cpu = new(Embedded: true, Gpu: false, VramMb: null, CpuCores: 4);
    private static readonly PlatformCaps GpuBox = new(Embedded: true, Gpu: true, VramMb: 16000, CpuCores: 8);
    private static readonly PlatformCaps NoEmbed = new(Embedded: false, Gpu: false, VramMb: null, CpuCores: 4);

    // --- Compatible -------------------------------------------------------

    [Fact]
    public void Online_IsCompatibleEverywhere()
    {
        var i = new LlmIntegration { Kind = "online", Provider = "custom", Endpoint = "http://x/v1" };
        Assert.True(i.Compatible(Cpu));
        Assert.True(i.Compatible(NoEmbed));
    }

    [Fact]
    public void Offline_NeedsEmbedded_AndGpuOnlyWhenDeviceIsGpu()
    {
        var cpu = new LlmIntegration { Kind = "offline", OfflineModel = "qwen2.5-1.5b", Device = "cpu" };
        Assert.True(cpu.Compatible(Cpu));
        Assert.False(cpu.Compatible(NoEmbed));

        var gpu = new LlmIntegration { Kind = "offline", OfflineModel = "qwen2.5-7b", Device = "gpu" };
        Assert.False(gpu.Compatible(Cpu));    // no gpu
        Assert.True(gpu.Compatible(GpuBox));
    }

    // --- Configured -------------------------------------------------------

    [Fact]
    public void OnlineCustomEndpoint_NeedsNoKey()
    {
        var ollama = new LlmIntegration { Kind = "online", Provider = "custom", Endpoint = "http://localhost:11434/v1" };
        Assert.True(ollama.Configured());
    }

    [Fact]
    public void OnlineHostedProvider_NeedsKey()
    {
        var noKey = new LlmIntegration { Kind = "online", Provider = "anthropic" };
        Assert.False(noKey.Configured());   // key comes from env; none set here

        var withKey = new LlmIntegration { Kind = "online", Provider = "anthropic", ApiKey = "sk-test" };
        Assert.True(withKey.Configured());
    }

    [Fact]
    public void Offline_ConfiguredWhenModelIdKnown()
    {
        Assert.True(new LlmIntegration { Kind = "offline", OfflineModel = "qwen2.5-1.5b" }.Configured());
        Assert.False(new LlmIntegration { Kind = "offline", OfflineModel = "made-up" }.Configured());
    }

    // --- Build ------------------------------------------------------------

    [Fact]
    public void Build_Online_UsesProviderEndpointAndDefaultModel()
    {
        var backend = new LlmIntegration { Kind = "online", Provider = "openai", ApiKey = "k" }.Build(0.2);
        Assert.NotNull(backend);
        Assert.Contains("openai", backend!.Describe());
        Assert.Contains("gpt-4o-mini", backend.Describe());  // provider default model
    }

    [Fact]
    public void Offline_RuntimeIsAvailable()
    {
        // Do NOT call Build() on an offline integration here — it would download a model.
        Assert.True(EmbeddedBackend.IsAvailable);
    }

    // --- Registry resolution ---------------------------------------------

    [Fact]
    public void Seeded_IsLocalFirst()
    {
        var ids = LlmConfig.Seeded().Integrations.Select(i => i.Id).ToList();
        Assert.Equal("local-gpu", ids[0]);                                    // on-device first
        Assert.True(ids.IndexOf("local-cpu") < ids.IndexOf("anthropic"));     // offline before cloud
        Assert.True(ids.IndexOf("ollama") < ids.IndexOf("anthropic"));        // local endpoint before cloud
    }

    [Fact]
    public void ResolveAll_KeylessSeeded_YieldsLocalOnly()
    {
        // With no API keys in env and no GPU, the active set is the keyless locals:
        // on-device CPU then local Ollama (the GPU entry is filtered out; cloud needs keys).
        var active = LlmConfig.Seeded().ResolveAll(Cpu).Select(i => i.Id).ToList();
        Assert.Equal(new[] { "local-cpu", "ollama" }, active);
        Assert.Equal("local-cpu", LlmConfig.Seeded().Resolve(Cpu)!.Id);
    }

    // --- Autotune / model sizing -----------------------------------------

    [Theory]
    [InlineData(16000, "qwen2.5-7b")]   // RTX 5070 Ti class
    [InlineData(8000, "qwen2.5-3b")]
    [InlineData(6000, "qwen2.5-1.5b")]
    [InlineData(3000, "qwen2.5-0.5b")]
    public void RecommendModel_GpuScalesWithVram(int vram, string expected)
    {
        var caps = new PlatformCaps(Embedded: true, Gpu: true, VramMb: vram, CpuCores: 8);
        Assert.Equal(expected, PlatformCaps.RecommendModel("offline", "gpu", caps));
    }

    [Theory]
    [InlineData(8, "qwen2.5-1.5b")]
    [InlineData(4, "qwen2.5-0.5b")]
    public void RecommendModel_CpuScalesWithCores(int cores, string expected)
    {
        var caps = new PlatformCaps(Embedded: true, Gpu: false, VramMb: null, CpuCores: cores);
        Assert.Equal(expected, PlatformCaps.RecommendModel("offline", "cpu", caps));
    }

    [Fact]
    public void Autotuned_SizesOfflineEntriesToCaps()
    {
        var cfg = LlmConfig.Autotuned(GpuBox);   // 16GB VRAM, 8 cores
        Assert.True(cfg.Auto);
        Assert.Equal("qwen2.5-7b", cfg.Integrations.Single(i => i.Id == "local-gpu").OfflineModel);
        Assert.Equal("qwen2.5-1.5b", cfg.Integrations.Single(i => i.Id == "local-cpu").OfflineModel);
    }
}
