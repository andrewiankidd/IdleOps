using System.Net.Http;

namespace llmctl.Llm;

/// <summary>
/// Downloads a GGUF model from Hugging Face into a local cache on first use.
/// (llama.cpp/LLamaSharp reads the tokenizer embedded in the GGUF, so unlike
/// candle we don't need the separate tokenizer repo.)
/// </summary>
internal static class ModelDownloader
{
    public static string CacheDir =>
        Environment.GetEnvironmentVariable("IDLEOPS_LLM_CACHE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "idleops", "models");

    /// <summary>Return the local path to the model's GGUF, downloading it if absent.</summary>
    public static string EnsureModel(OfflineModel model)
    {
        Directory.CreateDirectory(CacheDir);
        var path = Path.Combine(CacheDir, model.File);
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            return path;
        }

        var url = $"https://huggingface.co/{model.Repo}/resolve/main/{model.File}";
        Console.Error.WriteLine($"[llmctl] downloading {model.Id} (~{model.SizeMb} MB) — one-time, from {model.Repo}");
        Download(url, path);
        Console.Error.WriteLine($"[llmctl] cached at {path}");
        return path;
    }

    private static void Download(string url, string destPath)
    {
        var tmp = destPath + ".part";
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var resp = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength;

        using (var src = resp.Content.ReadAsStream())
        using (var dst = File.Create(tmp))
        {
            var buffer = new byte[1 << 20];
            long read = 0;
            var lastPct = -1;
            int n;
            while ((n = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dst.Write(buffer, 0, n);
                read += n;
                if (total is long t and > 0)
                {
                    var pct = (int)(read * 100 / t);
                    if (pct != lastPct && pct % 5 == 0)
                    {
                        lastPct = pct;
                        Console.Error.Write($"\r[llmctl] downloading… {pct,3}%  ({read >> 20}/{t >> 20} MB)");
                    }
                }
            }
        }
        Console.Error.WriteLine();
        File.Move(tmp, destPath, overwrite: true);
    }
}
