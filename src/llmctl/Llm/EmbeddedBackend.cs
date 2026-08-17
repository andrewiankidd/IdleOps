using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace llmctl.Llm;

/// <summary>
/// Embedded in-process GGUF backend (offline, no external server) via LLamaSharp —
/// the .NET equivalent of POSEIDON's candle path. Text-only; vision tasks fall
/// through to an online/Ollama VL model. The model is auto-downloaded on first use.
/// </summary>
internal sealed class EmbeddedBackend : IChatBackend, IDisposable
{
    private readonly LLamaWeights _weights;
    private readonly StatelessExecutor _executor;
    private readonly string _label;
    private readonly float _temperature;

    private EmbeddedBackend(LLamaWeights weights, ModelParams modelParams, string label, float temperature)
    {
        _weights = weights;
        _executor = new StatelessExecutor(weights, modelParams);
        _label = label;
        _temperature = temperature;
    }

    // The runtime is present (package referenced); actual load can still fail (e.g. download).
    public static bool IsAvailable => true;
    public bool SupportsVision => false;
    public string Describe() => _label;

    private static bool _logConfigured;

    // Silence llama.cpp's verbose native logging; keep warnings/errors on stderr.
    private static void QuietNativeLogs()
    {
        if (_logConfigured) return;
        _logConfigured = true;
        try
        {
            NativeLogConfig.llama_log_set((level, message) =>
            {
                if (level is LLamaLogLevel.Error or LLamaLogLevel.Warning)
                {
                    Console.Error.Write(message);
                }
            });
        }
        catch
        {
            // best-effort
        }
    }

    public static IChatBackend? TryCreate(string offlineModelId, string device, float temperature)
    {
        var preset = Presets.OfflineModelById(offlineModelId);
        if (preset is null) return null;
        try
        {
            QuietNativeLogs();
            var path = ModelDownloader.EnsureModel(preset);
            var modelParams = new ModelParams(path)
            {
                ContextSize = 4096,
                GpuLayerCount = device == "gpu" ? 999 : 0,
            };
            var weights = LLamaWeights.LoadFromFile(modelParams);
            return new EmbeddedBackend(weights, modelParams, $"offline:{offlineModelId}", temperature);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[llmctl] offline model '{offlineModelId}' unavailable: {ex.Message}");
            return null;
        }
    }

    public async Task<string> CompleteAsync(string? system, string userText, string? imageBase64, CancellationToken token)
    {
        if (imageBase64 is not null)
        {
            throw new NotSupportedException("The offline model is text-only; use a vision backend for image tasks.");
        }

        var prompt = BuildQwenPrompt(system, userText);
        var infer = new InferenceParams
        {
            MaxTokens = 512,
            AntiPrompts = ["<|im_end|>"],
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = _temperature },
        };

        var sb = new StringBuilder();
        await foreach (var chunk in _executor.InferAsync(prompt, infer, token))
        {
            sb.Append(chunk);
        }
        return sb.ToString().Replace("<|im_end|>", string.Empty).Trim();
    }

    // Qwen2.5 chat template.
    private static string BuildQwenPrompt(string? system, string user)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(system))
        {
            sb.Append("<|im_start|>system\n").Append(system).Append("<|im_end|>\n");
        }
        sb.Append("<|im_start|>user\n").Append(user).Append("<|im_end|>\n<|im_start|>assistant\n");
        return sb.ToString();
    }

    public void Dispose() => _weights.Dispose();
}
