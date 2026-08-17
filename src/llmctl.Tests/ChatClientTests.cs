using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using llmctl.Llm;
using Xunit;

namespace llmctl.Tests;

public class ChatClientTests
{
    // --- BuildRequestBody (pure) ------------------------------------------

    [Fact]
    public void BuildRequestBody_TextOnly_UsesStringContent()
    {
        var json = ChatClient.BuildRequestBody("m", system: null, "hello", imageBase64: null, 0.2);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("m", root.GetProperty("model").GetString());
        var msg = root.GetProperty("messages")[0];
        Assert.Equal("user", msg.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.String, msg.GetProperty("content").ValueKind);
        Assert.Equal("hello", msg.GetProperty("content").GetString());
    }

    [Fact]
    public void BuildRequestBody_WithSystem_PrependsSystemMessage()
    {
        var json = ChatClient.BuildRequestBody("m", "be terse", "hi", null, 0.2);
        using var doc = JsonDocument.Parse(json);
        var messages = doc.RootElement.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("be terse", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
    }

    [Fact]
    public void BuildRequestBody_WithImage_EmitsImageUrlDataContent()
    {
        var json = ChatClient.BuildRequestBody("m", null, "what is this?", "QUJD", 0.2); // "ABC" base64
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("messages")[0].GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.Equal("text", content[0].GetProperty("type").GetString());
        Assert.Equal("image_url", content[1].GetProperty("type").GetString());
        Assert.Equal("data:image/png;base64,QUJD", content[1].GetProperty("image_url").GetProperty("url").GetString());
    }

    // --- ExtractContent (pure) --------------------------------------------

    [Fact]
    public void ExtractContent_PullsAssistantMessage()
    {
        var response = """
            {"choices":[{"message":{"role":"assistant","content":"the answer"}}]}
            """;
        Assert.Equal("the answer", ChatClient.ExtractContent(response));
    }

    // --- CompleteAsync (full send/parse via a stub handler) ---------------

    [Fact]
    public async Task CompleteAsync_SendsRequest_AndParsesReply()
    {
        var stub = new StubHandler("""
            {"choices":[{"message":{"content":"hi back"}}]}
            """);
        var http = new HttpClient(stub);
        var client = new ChatClient("http://fake/v1", "m", apiKey: "k", temperature: 0.2, http: http);

        var reply = await client.CompleteAsync("sys", "hi", imageBase64: null, CancellationToken.None);

        Assert.Equal("hi back", reply);
        Assert.Equal("http://fake/v1/chat/completions", stub.RequestUri);
        Assert.Contains("\"model\":\"m\"", stub.RequestBody);
        Assert.Equal("Bearer k", stub.AuthHeader);
    }

    [Fact]
    public async Task CompleteAsync_NonSuccess_Throws()
    {
        var stub = new StubHandler("boom", HttpStatusCode.InternalServerError);
        var client = new ChatClient("http://fake/v1", "m", null, 0.2, new HttpClient(stub));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CompleteAsync(null, "hi", null, CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _status;
        public string? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = "";
        public string? AuthHeader { get; private set; }

        public StubHandler(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _status = status;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            AuthHeader = request.Headers.Authorization?.ToString();
            RequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_responseBody) };
        }
    }
}
