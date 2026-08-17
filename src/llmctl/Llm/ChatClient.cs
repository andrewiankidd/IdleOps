using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace llmctl.Llm;

/// <summary>
/// Minimal client for the OpenAI-compatible /chat/completions API (Ollama, vLLM,
/// LM Studio, ...). Supports a single-turn text + optional image (vision) prompt.
/// </summary>
internal sealed class ChatClient
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _model;
    private readonly double _temperature;

    public ChatClient(string endpoint, string model, string? apiKey, double temperature, HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _url = endpoint.TrimEnd('/') + "/chat/completions";
        _model = model;
        _temperature = temperature;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<string> CompleteAsync(string? system, string userText, string? imageBase64, CancellationToken token)
    {
        var body = BuildRequestBody(_model, system, userText, imageBase64, _temperature);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(_url, content, token);
        var json = await response.Content.ReadAsStringAsync(token);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LLM endpoint returned {(int)response.StatusCode}: {Truncate(json, 300)}");
        }

        return ExtractContent(json);
    }

    /// <summary>Build the chat-completions request JSON. Pure — unit testable.</summary>
    public static string BuildRequestBody(string model, string? system, string userText, string? imageBase64, double temperature)
    {
        object userContent = imageBase64 is null
            ? userText
            : new object[]
            {
                new { type = "text", text = userText },
                new { type = "image_url", image_url = new { url = $"data:image/png;base64,{imageBase64}" } },
            };

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(system))
        {
            messages.Add(new { role = "system", content = system });
        }
        messages.Add(new { role = "user", content = userContent });

        return JsonSerializer.Serialize(new
        {
            model,
            messages,
            temperature,
            stream = false,
        });
    }

    /// <summary>Pull choices[0].message.content out of a chat-completions response. Pure — unit testable.</summary>
    public static string ExtractContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
