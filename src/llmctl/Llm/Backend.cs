namespace llmctl.Llm;

/// <summary>A resolved, callable AI backend (online HTTP or embedded in-process).</summary>
internal interface IChatBackend
{
    bool SupportsVision { get; }
    string Describe();
    Task<string> CompleteAsync(string? system, string userText, string? imageBase64, CancellationToken token);
}

/// <summary>Online backend over the OpenAI-compatible ChatClient (cloud or local endpoint).</summary>
internal sealed class OnlineBackend : IChatBackend
{
    private readonly ChatClient _client;
    private readonly string _label;

    public OnlineBackend(string endpoint, string model, string? apiKey, double temperature, string label)
    {
        _client = new ChatClient(endpoint, model, apiKey, temperature);
        _label = label;
    }

    public bool SupportsVision => true;  // depends on the model; the caller picks a VL model for image tasks
    public string Describe() => _label;

    public Task<string> CompleteAsync(string? system, string userText, string? imageBase64, CancellationToken token)
        => _client.CompleteAsync(system, userText, imageBase64, token);
}
