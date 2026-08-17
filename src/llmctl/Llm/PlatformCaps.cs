namespace llmctl.Llm;

/// <summary>
/// What this machine can run — drives integration compatibility, priority, and
/// model sizing (ported from POSEIDEN's PlatformCaps; webgpu dropped since IdleOps
/// isn't browser). VRAM/cores let a strong box pick a strong model out of the box
/// and a weak one stay fast.
/// </summary>
internal sealed record PlatformCaps(bool Embedded, bool Gpu, int? VramMb, int? CpuCores)
{
    public static PlatformCaps Detect()
    {
        var embedded = OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
        var vram = QueryVramMb();
        var gpu = QueryGpuForced() ?? vram is not null;
        return new PlatformCaps(embedded, gpu, vram, Environment.ProcessorCount);
    }

    private static bool? QueryGpuForced()
    {
        var forced = Environment.GetEnvironmentVariable("IDLEOPS_LLM_GPU");
        if (forced is null) return null;
        return forced == "1" || forced.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Total GPU VRAM in MB via nvidia-smi, or null if no NVIDIA GPU / not readable.</summary>
    private static int? QueryVramMb()
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(2000) || p.ExitCode != 0) return null;
            var first = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            return int.TryParse(first, out var mb) ? mb : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Highest reasonably-runnable offline model id for this kind/device/caps —
    /// so a strong box gets a strong model and a weak one stays fast (ported from
    /// POSEIDEN's recommend_model).
    /// </summary>
    public static string RecommendModel(string kind, string device, PlatformCaps caps)
    {
        static string ByVram(int v) =>
            v >= 12000 ? "qwen2.5-7b"
            : v >= 7000 ? "qwen2.5-3b"
            : v >= 4000 ? "qwen2.5-1.5b"
            : "qwen2.5-0.5b";

        if (kind == "offline" && device == "gpu")
        {
            return caps.VramMb is int v ? ByVram(v) : "qwen2.5-3b";
        }
        if (kind == "offline")
        {
            return caps.CpuCores is int c && c >= 8 ? "qwen2.5-1.5b" : "qwen2.5-0.5b";
        }
        return "qwen2.5-0.5b";
    }
}
