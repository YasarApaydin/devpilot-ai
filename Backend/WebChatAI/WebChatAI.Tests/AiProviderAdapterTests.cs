using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Infrastructure.Services.AiProviders;
using Xunit;

namespace WebChatAI.Tests;

public class AiProviderAdapterTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public List<HttpRequestMessage> Requests { get; } = new();
        public HttpRequestMessage? LastRequest => Requests.LastOrDefault();
        public string? LastRequestBody { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _handlerFunc = async (req, ct) =>
            {
                Requests.Add(req);
                if (req.Content != null)
                {
                    LastRequestBody = await req.Content.ReadAsStringAsync(ct);
                }
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            };
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = async (req, ct) =>
            {
                Requests.Add(req);
                if (req.Content != null)
                {
                    LastRequestBody = await req.Content.ReadAsStringAsync(ct);
                }
                return await handlerFunc(req, ct);
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handlerFunc(request, cancellationToken);
    }

    [Fact]
    public void AiProviderRegistry_ResolvesAllProvidersCaseInsensitively()
    {
        var adapters = new List<IAiProviderAdapter>
        {
            new OpenAiProviderAdapter(new HttpClient(), NullLogger<OpenAiProviderAdapter>.Instance),
            new GeminiProviderAdapter(new HttpClient(), NullLogger<GeminiProviderAdapter>.Instance),
            new GroqProviderAdapter(new HttpClient(), NullLogger<GroqProviderAdapter>.Instance),
            new XAiProviderAdapter(new HttpClient(), NullLogger<XAiProviderAdapter>.Instance),
            new AnthropicProviderAdapter(new HttpClient(), NullLogger<AnthropicProviderAdapter>.Instance),
            new DeepSeekProviderAdapter(new HttpClient(), NullLogger<DeepSeekProviderAdapter>.Instance),
            new OpenRouterProviderAdapter(new HttpClient(), NullLogger<OpenRouterProviderAdapter>.Instance),
            new MistralProviderAdapter(new HttpClient(), NullLogger<MistralProviderAdapter>.Instance),
            new SystemDefaultAiProviderAdapter(null!)
        };

        var registry = new AiProviderRegistry(adapters);

        Assert.NotNull(registry.GetAdapter("openai"));
        Assert.NotNull(registry.GetAdapter("OPENAI"));
        Assert.NotNull(registry.GetAdapter("Gemini"));
        Assert.NotNull(registry.GetAdapter("GROQ"));
        Assert.NotNull(registry.GetAdapter("xai"));
        Assert.NotNull(registry.GetAdapter("anthropic"));
        Assert.NotNull(registry.GetAdapter("deepseek"));
        Assert.NotNull(registry.GetAdapter("DEEPSEEK"));
        Assert.NotNull(registry.GetAdapter("openrouter"));
        Assert.NotNull(registry.GetAdapter("OPENROUTER"));
        Assert.NotNull(registry.GetAdapter("mistral"));
        Assert.NotNull(registry.GetAdapter("MISTRAL"));
        Assert.NotNull(registry.GetAdapter("system"));
        Assert.Null(registry.GetAdapter("unknown_provider"));
    }

    [Fact]
    public async Task OpenAiProvider_WhenApiKeyInvalid_ReturnsFriendly401Error()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"error\": {\"message\": \"Incorrect API key provided\"}}");
        var client = new HttpClient(handler);
        var adapter = new OpenAiProviderAdapter(client, NullLogger<OpenAiProviderAdapter>.Instance);

        var result = await adapter.TestConnectionAsync("sk-invalid-test-key");

        Assert.False(result.Success);
        Assert.Contains("401", result.Message);
        Assert.DoesNotContain("sk-invalid-test-key", result.Message);
    }

    [Fact]
    public async Task OpenAiProvider_WhenQuotaExceeded_ReturnsFriendly429Error()
    {
        var handler = new MockHttpMessageHandler((HttpStatusCode)429, "{\"error\": {\"message\": \"You exceeded your current quota\"}}");
        var client = new HttpClient(handler);
        var adapter = new OpenAiProviderAdapter(client, NullLogger<OpenAiProviderAdapter>.Instance);

        var result = await adapter.TestConnectionAsync("sk-quota-exceeded-key");

        Assert.False(result.Success);
        Assert.Contains("429", result.Message);
        Assert.Contains("kota", result.Message.ToLower());
    }

    [Fact]
    public async Task AnthropicProvider_WhenQuotaExceeded_ReturnsFriendly429Error()
    {
        var handler = new MockHttpMessageHandler((HttpStatusCode)429, "{\"error\": {\"message\": \"usage_limit_reached\"}}");
        var client = new HttpClient(handler);
        var adapter = new AnthropicProviderAdapter(client, NullLogger<AnthropicProviderAdapter>.Instance);

        var result = await adapter.TestConnectionAsync("sk-ant-test-key");

        Assert.False(result.Success);
        Assert.Contains("429", result.Message);
        Assert.Contains("bakiye veya hız", result.Message.ToLower());
    }

    [Fact]
    public async Task AnthropicProvider_WhenNotScopedToWorkspace_ReturnsHelpfulWorkspaceGuidance()
    {
        var errorJson = "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":\"This API key is not scoped to a workspace, so this request must include the anthropic-workspace-id header with the ID of the workspace to use. Add the header, or use an API key that is scoped to a workspace.\"},\"request_id\":\"req_011Cg4brAa9My36Ro\"}";
        var handler = new MockHttpMessageHandler(HttpStatusCode.BadRequest, errorJson);
        var client = new HttpClient(handler);
        var adapter = new AnthropicProviderAdapter(client, NullLogger<AnthropicProviderAdapter>.Instance);

        var result = await adapter.TestConnectionAsync("sk-ant-unscoped-key");

        Assert.False(result.Success);
        Assert.Contains("Workspace", result.Message);
        Assert.Contains("console.anthropic.com", result.Message);
        Assert.Contains("#workspace_id", result.Message);
    }

    [Fact]
    public void AnthropicProvider_ParseApiKeyAndWorkspace_ParsesDelimitersCorrectly()
    {
        var (k1, w1) = AnthropicProviderAdapter.ParseApiKeyAndWorkspace("sk-ant-api03-xxx#wrkspc_123");
        Assert.Equal("sk-ant-api03-xxx", k1);
        Assert.Equal("wrkspc_123", w1);

        var (k2, w2) = AnthropicProviderAdapter.ParseApiKeyAndWorkspace("sk-ant-api03-xxx|wrkspc_456");
        Assert.Equal("sk-ant-api03-xxx", k2);
        Assert.Equal("wrkspc_456", w2);

        var (k3, w3) = AnthropicProviderAdapter.ParseApiKeyAndWorkspace("sk-ant-api03-xxx;workspace=wrkspc_789");
        Assert.Equal("sk-ant-api03-xxx", k3);
        Assert.Equal("wrkspc_789", w3);

        var (k4, w4) = AnthropicProviderAdapter.ParseApiKeyAndWorkspace("sk-ant-api03-xxx");
        Assert.Equal("sk-ant-api03-xxx", k4);
        Assert.Null(w4);
    }

    [Fact]
    public void GeminiProvider_NormalizeGeminiModel_MapsLegacyAndGemmaModels()
    {
        Assert.Equal("gemini-3.8-flash", GeminiProviderAdapter.NormalizeGeminiModel("gemini-2.5-flash"));
        Assert.Equal("gemini-3.8-flash", GeminiProviderAdapter.NormalizeGeminiModel("gemini-2.0-flash"));
        Assert.Equal("gemini-3.8-flash", GeminiProviderAdapter.NormalizeGeminiModel("gemini-1.5-flash"));
        Assert.Equal("gemini-3.1-pro-preview", GeminiProviderAdapter.NormalizeGeminiModel("gemini-2.5-pro"));
        Assert.Equal("gemini-3.1-pro-preview", GeminiProviderAdapter.NormalizeGeminiModel("gemini-1.5-pro"));
        Assert.Equal("gemini-3.8-flash", GeminiProviderAdapter.NormalizeGeminiModel("gemini 3.8"));
        Assert.Equal("gemini-3.1-pro-preview", GeminiProviderAdapter.NormalizeGeminiModel("gemini 3.1 pro"));
        Assert.Equal("gemini-3-flash-preview", GeminiProviderAdapter.NormalizeGeminiModel("gemini 3 flash preview"));
        Assert.Equal("gemini-3.1-flash-lite-preview", GeminiProviderAdapter.NormalizeGeminiModel("gemini 3.1 flash lite"));
        Assert.Equal("gemini-3.5-flash-lite", GeminiProviderAdapter.NormalizeGeminiModel("gemini 3.5 flash lite"));
        Assert.Equal("gemini-3.8-flash", GeminiProviderAdapter.NormalizeGeminiModel(null));

        // Open-weight Gemma models via Gemini API
        Assert.Equal("gemma-4-31b-it", GeminiProviderAdapter.NormalizeGeminiModel("gemma-4-31b-it"));
        Assert.Equal("gemma-4-31b-it", GeminiProviderAdapter.NormalizeGeminiModel("models/gemma-4-31b-it"));
        Assert.Equal("gemma-4-26b-a4b-it", GeminiProviderAdapter.NormalizeGeminiModel("gemma-4-26b-a4b-it"));
        Assert.Equal("gemma-3-27b-it", GeminiProviderAdapter.NormalizeGeminiModel("gemma-3-27b-it"));
    }

    [Fact]
    public void GroqProvider_NormalizeGroqModel_MapsLegacyModels()
    {
        var adapter = new GroqProviderAdapter(new HttpClient(), NullLogger<GroqProviderAdapter>.Instance);
        Assert.DoesNotContain(adapter.DefaultModels, m => m.Id.Contains("qwen", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("openai/gpt-oss-120b", GroqProviderAdapter.NormalizeGroqModel("llama-3.3-70b-versatile"));
        Assert.Equal("openai/gpt-oss-120b", GroqProviderAdapter.NormalizeGroqModel("llama-3.1-70b-versatile"));
        Assert.Equal("openai/gpt-oss-120b", GroqProviderAdapter.NormalizeGroqModel("deepseek-r1-distill-llama-70b"));
        Assert.Equal("openai/gpt-oss-120b", GroqProviderAdapter.NormalizeGroqModel("mixtral-8x7b-32768"));
        Assert.Equal("openai/gpt-oss-20b", GroqProviderAdapter.NormalizeGroqModel("llama-3.1-8b-instant"));
        Assert.Equal("openai/gpt-oss-120b", GroqProviderAdapter.NormalizeGroqModel(null));
    }

    [Fact]
    public void DeepSeekProvider_NormalizeDeepSeekModel_MapsAliasesAndDefaults()
    {
        Assert.Equal("deepseek-chat", DeepSeekProviderAdapter.NormalizeDeepSeekModel(null));
        Assert.Equal("deepseek-chat", DeepSeekProviderAdapter.NormalizeDeepSeekModel("deepseek-v3"));
        Assert.Equal("deepseek-reasoner", DeepSeekProviderAdapter.NormalizeDeepSeekModel("deepseek-r1"));
        Assert.Equal("deepseek-reasoner", DeepSeekProviderAdapter.NormalizeDeepSeekModel("deepseek-reasoner"));
        Assert.Equal("deepseek-flash", DeepSeekProviderAdapter.NormalizeDeepSeekModel("deepseek-v4.1-flash"));
        Assert.Equal("deepseek-v4-pro", DeepSeekProviderAdapter.NormalizeDeepSeekModel("deepseek-v4-pro"));
    }

    [Fact]
    public void OpenRouterProvider_NormalizeOpenRouterModel_MapsProviderPrefixedModels()
    {
        Assert.Equal("deepseek/deepseek-v4.1-flash", OpenRouterProviderAdapter.NormalizeOpenRouterModel(null));
        Assert.Equal("mistralai/devstral-2512", OpenRouterProviderAdapter.NormalizeOpenRouterModel("devstral-2512"));
        Assert.Equal("google/gemma-4-31b-it:free", OpenRouterProviderAdapter.NormalizeOpenRouterModel("gemma-4-free"));
        Assert.Equal("google/gemma-4-31b-it", OpenRouterProviderAdapter.NormalizeOpenRouterModel("gemma-4-31b-it"));
        Assert.Equal("google/gemma-3-27b-it", OpenRouterProviderAdapter.NormalizeOpenRouterModel("gemma-3-27b-it"));
        Assert.Equal("deepseek/deepseek-r1", OpenRouterProviderAdapter.NormalizeOpenRouterModel("deepseek-reasoner"));
    }

    [Fact]
    public void MistralProvider_NormalizeMistralModel_MapsRetiredDevstralToActiveReplacement()
    {
        Assert.Equal("mistral-medium-3-5", MistralProviderAdapter.NormalizeMistralModel(null));
        Assert.Equal("mistral-medium-3-5", MistralProviderAdapter.NormalizeMistralModel("devstral-2512"));
        Assert.Equal("mistral-medium-3-5", MistralProviderAdapter.NormalizeMistralModel("devstral-small-2505"));
        Assert.Equal("mistral-large-4", MistralProviderAdapter.NormalizeMistralModel("mistral-large-latest"));
        Assert.Equal("codestral-2508", MistralProviderAdapter.NormalizeMistralModel("codestral-latest"));
        Assert.Equal("mistral-small-2603", MistralProviderAdapter.NormalizeMistralModel("mistral-small-latest"));
    }

    [Theory]
    [InlineData("deepseek", "sk-invalid-deepseek")]
    [InlineData("openrouter", "sk-or-v1-invalid-key")]
    [InlineData("mistral", "invalid-mistral-key")]
    public async Task NewProviders_WhenApiKeyMissingOrInvalid_ReturnsFriendlyErrorWithoutLeakingKey(string provider, string secretKey)
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"Authentication Fails\"}}");
        var client = new HttpClient(handler);

        IAiProviderAdapter adapter = provider switch
        {
            "deepseek" => new DeepSeekProviderAdapter(client, NullLogger<DeepSeekProviderAdapter>.Instance),
            "openrouter" => new OpenRouterProviderAdapter(client, NullLogger<OpenRouterProviderAdapter>.Instance),
            "mistral" => new MistralProviderAdapter(client, NullLogger<MistralProviderAdapter>.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        var emptyResult = await adapter.TestConnectionAsync("   ");
        Assert.False(emptyResult.Success);

        var invalidResult = await adapter.TestConnectionAsync(secretKey);
        Assert.False(invalidResult.Success);
        Assert.Contains("401", invalidResult.Message);
        Assert.DoesNotContain(secretKey, invalidResult.Message);
    }

    [Fact]
    public async Task DeepSeekProvider_WhenInsufficientBalance402_ReturnsFriendlyBalanceError()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.PaymentRequired, "{\"error\":{\"message\":\"Insufficient Balance\"}}");
        var client = new HttpClient(handler);
        var adapter = new DeepSeekProviderAdapter(client, NullLogger<DeepSeekProviderAdapter>.Instance);

        var testResult = await adapter.TestConnectionAsync("sk-deepseek-zero-balance");
        Assert.False(testResult.Success);
        Assert.Contains("402", testResult.Message);
        Assert.Contains("bakiye", testResult.Message.ToLowerInvariant());

        var chatResponse = await adapter.GenerateChatResponseAsync(new List<Message>(), "Merhaba", "deepseek-chat", "sk-deepseek-zero-balance");
        Assert.Contains("402", chatResponse);
        Assert.Contains("bakiye", chatResponse.ToLowerInvariant());
    }

    [Fact]
    public async Task OpenRouterProvider_TestConnection_UsesAuthKeyEndpointAndValidatesSuccessfully()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{\"data\":{\"label\":\"devpilot-key\",\"usage\":0}}");
        var client = new HttpClient(handler);
        var adapter = new OpenRouterProviderAdapter(client, NullLogger<OpenRouterProviderAdapter>.Instance);

        var result = await adapter.TestConnectionAsync("sk-or-v1-valid-key");

        Assert.True(result.Success);
        Assert.NotEmpty(handler.Requests);
        Assert.Equal("https://openrouter.ai/api/v1/auth/key", handler.Requests[0].RequestUri!.ToString());
        Assert.NotNull(result.Models);
        Assert.Contains(result.Models!, m => m.Id == "mistralai/devstral-2512");
        Assert.Contains(result.Models!, m => m.Id == "google/gemma-4-31b-it:free");
    }

    [Fact]
    public async Task MistralProvider_WhenTimeoutOr5xxOccurs_ReturnsFriendlyTurkishError()
    {
        var timeoutHandler = new MockHttpMessageHandler((_, _) => throw new TaskCanceledException("Request timed out"));
        var timeoutAdapter = new MistralProviderAdapter(new HttpClient(timeoutHandler), NullLogger<MistralProviderAdapter>.Instance);

        var timeoutResponse = await timeoutAdapter.GenerateChatResponseAsync(new List<Message>(), "Kod analizi yap", "mistral-large-4", "valid-key");
        Assert.Contains("zaman aşımı", timeoutResponse.ToLowerInvariant());

        var serverErrorHandler = new MockHttpMessageHandler(HttpStatusCode.BadGateway, "{\"message\":\"Upstream gateway error\"}");
        var serverErrorAdapter = new MistralProviderAdapter(new HttpClient(serverErrorHandler), NullLogger<MistralProviderAdapter>.Instance);

        var errorResponse = await serverErrorAdapter.GenerateChatResponseAsync(new List<Message>(), "Kod analizi yap", "mistral-large-4", "valid-key");
        Assert.Contains("502", errorResponse);
        Assert.Contains("API Hatası", errorResponse);
    }

    [Fact]
    public async Task DeepSeekProvider_StreamsLongTurkishResponseAndCodeAnalysisCorrectly()
    {
        var ssePayload = string.Join("\n", new[]
        {
            "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"Kod güvenlik açısından inceleniyor...\"}}]}",
            "",
            "data: {\"choices\":[{\"delta\":{\"content\":\"### Kod Analizi Özeti\\n\"}}]}",
            "",
            "data: {\"choices\":[{\"delta\":{\"content\":\"Bu C# metodunda **SQL Injection** zafiyeti bulunmamaktadır; parametreli sorgu ve `AES-256-GCM` şifrelemesi doğru uygulanmıştır.\"}}]}",
            "",
            "data: [DONE]",
            ""
        });

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, ssePayload);
        var adapter = new DeepSeekProviderAdapter(new HttpClient(handler), NullLogger<DeepSeekProviderAdapter>.Instance);

        var response = await adapter.GenerateChatResponseAsync(
            new List<Message>(),
            "Aşağıdaki C# kodunu güvenlik açısından incele ve Türkçe açıkla.",
            "deepseek-reasoner",
            "sk-valid-deepseek-key");

        Assert.Contains("Kod Analizi Özeti", response);
        Assert.Contains("SQL Injection", response);
        Assert.Contains("AES-256-GCM", response);
    }

    [Fact]
    public async Task GeminiProvider_WithGemmaModel_EmbedsSystemPromptInUserTurnAndStreamsResponse()
    {
        var ssePayload = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Merhaba! Ben Google Gemma 4 açık ağırlıklı modeliyim.\"}]}}]}\n\ndata: [DONE]\n";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, ssePayload);
        var adapter = new GeminiProviderAdapter(new HttpClient(handler), NullLogger<GeminiProviderAdapter>.Instance);

        var response = await adapter.GenerateChatResponseAsync(
            new List<Message>(),
            "Kendini tanıt.",
            "gemma-4-31b-it",
            "AIzaSyTestKey");

        Assert.Contains("Gemma 4", response);
        Assert.NotNull(handler.LastRequestBody);
        Assert.DoesNotContain("\"systemInstruction\"", handler.LastRequestBody!);
        Assert.Contains("Sistem Y", handler.LastRequestBody!);
    }
}
