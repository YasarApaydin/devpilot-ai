using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class AnthropicProviderAdapter : IAiProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProviderAdapter> _logger;

    public string Provider => "anthropic";
    public string DisplayName => "Anthropic Claude";
    public string DocUrl => "https://console.anthropic.com/settings/keys";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("claude-3-7-sonnet-20250219", "Claude 3.7 Sonnet", "anthropic", "Anthropic'in en yeni hibrit akıl yürütme ve analitik modeli.", true, 200000),
        new("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet", "anthropic", "Endüstri lideri kodlama, analiz ve yaratıcı yazım modeli.", false, 200000),
        new("claude-3-5-haiku-20241022", "Claude 3.5 Haiku", "anthropic", "Ultra hızlı, kompakt ve uygun maliyetli zeka.", false, 200000),
        new("claude-3-opus-20240229", "Claude 3 Opus", "anthropic", "Karmaşık problem çözme ve derin teknik muhakeme.", false, 200000)
    };

    public AnthropicProviderAdapter(HttpClient httpClient, ILogger<AnthropicProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var (cleanKey, workspaceId) = ParseApiKeyAndWorkspace(apiKey);

            // 1. Try GET /v1/models first
            using var modelRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
            ApplyAnthropicHeaders(modelRequest, cleanKey, workspaceId);

            using var modelResponse = await _httpClient.SendAsync(modelRequest, cancellationToken);
            if (modelResponse.IsSuccessStatusCode)
            {
                var content = await modelResponse.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseAnthropicModels(content);
                return new AiKeyTestResponseDto(true, "Anthropic Claude API anahtarı başarıyla doğrulandı.", models.Count > 0 ? models : DefaultModels.ToList());
            }

            var modelErrBody = await modelResponse.Content.ReadAsStringAsync(cancellationToken);

            // Check if Anthropic requires workspace scoping
            if (modelErrBody.Contains("not scoped to a workspace", StringComparison.OrdinalIgnoreCase))
            {
                return new AiKeyTestResponseDto(false, GetWorkspaceErrorMessage());
            }

            // 2. If models endpoint returned 401 or 403, fail immediately
            if (modelResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized || modelResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz Anthropic API anahtarı (401/403). Lütfen console.anthropic.com üzerinden anahtarınızı kontrol edin.");
            }

            // 3. Fallback: minimal message verification
            var pingBody = new
            {
                model = "claude-3-5-haiku-20241022",
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "ping" } }
            };

            using var pingRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            ApplyAnthropicHeaders(pingRequest, cleanKey, workspaceId);
            pingRequest.Content = new StringContent(JsonSerializer.Serialize(pingBody), Encoding.UTF8, "application/json");

            using var pingResponse = await _httpClient.SendAsync(pingRequest, cancellationToken);
            if (pingResponse.IsSuccessStatusCode)
            {
                return new AiKeyTestResponseDto(true, "Anthropic Claude API anahtarı başarıyla doğrulandı.", DefaultModels.ToList());
            }

            var errBody = await pingResponse.Content.ReadAsStringAsync(cancellationToken);

            if (errBody.Contains("not scoped to a workspace", StringComparison.OrdinalIgnoreCase))
            {
                return new AiKeyTestResponseDto(false, GetWorkspaceErrorMessage());
            }

            if (pingResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz Anthropic API anahtarı (HTTP 401).");
            }
            if ((int)pingResponse.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "Anthropic Claude API bakiye veya hız sınırına ulaşıldı (HTTP 429).");
            }

            return new AiKeyTestResponseDto(false, $"Anthropic API hatası: {pingResponse.StatusCode} - {errBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic test connection failed");
            return new AiKeyTestResponseDto(false, $"Anthropic API bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var (cleanKey, workspaceId) = ParseApiKeyAndWorkspace(apiKey);
            using var modelRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
            ApplyAnthropicHeaders(modelRequest, cleanKey, workspaceId);

            using var modelResponse = await _httpClient.SendAsync(modelRequest, cancellationToken);
            if (modelResponse.IsSuccessStatusCode)
            {
                var content = await modelResponse.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseAnthropicModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Anthropic models dynamically, using defaults");
        }

        return DefaultModels.ToList();
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (cleanKey, workspaceId) = ParseApiKeyAndWorkspace(apiKey);
        var targetModel = NormalizeAnthropicModel(model);

        var messagesPayload = BuildAnthropicMessages(history, userMessage, ragContext);

        var requestBody = new
        {
            model = targetModel,
            max_tokens = 4096,
            system = AiPromptHelper.SystemPrompt,
            messages = messagesPayload,
            stream = true,
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        ApplyAnthropicHeaders(request, cleanKey, workspaceId);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errText.Contains("not scoped to a workspace", StringComparison.OrdinalIgnoreCase))
            {
                yield return "Hata: Anthropic API anahtarınız bir Çalışma Alanına (Workspace) bağlı değil. Çözüm: console.anthropic.com adresinde sol üstten bir Workspace (örn: Default) seçerek 'API Keys' sekmesinden o Workspace içinde yeni anahtar oluşturun veya anahtarınızın sonuna #workspace_id ekleyin (Örn: sk-ant-...#wrkspc_xxx).";
                yield break;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                yield return "Hata: Anthropic API anahtarınız geçersiz (HTTP 401/403). Lütfen profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if ((int)response.StatusCode == 429)
            {
                yield return "Hata: Anthropic Claude API kotanız veya krediniz tükendi (HTTP 429). Lütfen console.anthropic.com bakiyenizi kontrol edin veya başka bir model seçin.";
                yield break;
            }

            yield return $"Anthropic Claude API Hatası ({response.StatusCode}): {errText}";
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]") break;

                string? deltaText = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var type = doc.RootElement.GetProperty("type").GetString();

                    if (type == "content_block_delta" && doc.RootElement.TryGetProperty("delta", out var delta))
                    {
                        if (delta.TryGetProperty("text", out var textProp))
                        {
                            deltaText = textProp.GetString();
                        }
                    }
                }
                catch
                {
                    // Ignore SSE json chunk fragment parsing
                }

                if (!string.IsNullOrEmpty(deltaText))
                {
                    yield return deltaText;
                }
            }
        }
    }

    public async Task<string> GenerateChatResponseAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in StreamChatAsync(history, userMessage, model, apiKey, ragContext, cancellationToken))
        {
            sb.Append(chunk);
        }
        return sb.ToString();
    }

    private static List<AiModelInfoDto> ParseAnthropicModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var el in data.EnumerateArray())
                {
                    var id = el.GetProperty("id").GetString();
                    if (!string.IsNullOrEmpty(id) && id.Contains("claude"))
                    {
                        var displayName = el.TryGetProperty("display_name", out var dn) ? dn.GetString() : id;
                        result.Add(new AiModelInfoDto(id, displayName ?? id, "anthropic", $"Anthropic {id}"));
                    }
                }
            }
        }
        catch
        {
            // fallback
        }
        return result;
    }

    public static (string ApiKey, string? WorkspaceId) ParseApiKeyAndWorkspace(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, null);

        var trimmed = input.Trim();

        if (trimmed.Contains(";workspace", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(';', 2, StringSplitOptions.TrimEntries);
            var key = parts[0];
            var right = parts[1];
            var eqIdx = right.IndexOf('=');
            var wsId = eqIdx >= 0 ? right[(eqIdx + 1)..].Trim() : right.Trim();
            return (key, string.IsNullOrWhiteSpace(wsId) ? null : wsId);
        }

        if (trimmed.Contains('#'))
        {
            var parts = trimmed.Split('#', 2, StringSplitOptions.TrimEntries);
            return (parts[0], string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]);
        }

        if (trimmed.Contains('|'))
        {
            var parts = trimmed.Split('|', 2, StringSplitOptions.TrimEntries);
            return (parts[0], string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]);
        }

        return (trimmed, null);
    }

    public static void ApplyAnthropicHeaders(HttpRequestMessage request, string apiKey, string? workspaceId)
    {
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        if (!string.IsNullOrWhiteSpace(workspaceId))
        {
            request.Headers.Add("anthropic-workspace-id", workspaceId);
        }
    }

    private static string GetWorkspaceErrorMessage() =>
        "Anthropic API Hatası: Bu API anahtarı bir Çalışma Alanına (Workspace) atanmamış.\n\n" +
        "Çözüm Seçenekleri:\n" +
        "1. (Önerilen): console.anthropic.com adresinde sol üstteki menüden bir Workspace (örn: 'Default' veya kendi çalışma alanınız) seçin. Sol menüden 'API Keys' sekmesine tıklayıp o çalışma alanı içinde yeni bir anahtar oluşturun.\n" +
        "2. Eğer organizasyon anahtarı kullanıyorsanız, anahtarınızı 'sk-ant-api03-...#workspace_id' formatında (sonuna # ve çalışma alanı ID'nizi ekleyerek, örn: #workspace_id veya #wrkspc_xxx) kaydedebilirsiniz.";

    public static string NormalizeAnthropicModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model)) return "claude-3-5-sonnet-20241022";
        var m = model.Trim().ToLowerInvariant();
        if (m == "claude-3-7-sonnet" || m.Contains("3.7-sonnet") || m.Contains("3-7-sonnet")) return "claude-3-7-sonnet-20250219";
        if (m == "claude-3-5-sonnet" || m.Contains("3.5-sonnet") || m.Contains("3-5-sonnet")) return "claude-3-5-sonnet-20241022";
        if (m == "claude-3-5-haiku" || m.Contains("3.5-haiku") || m.Contains("3-5-haiku")) return "claude-3-5-haiku-20241022";
        if (m == "claude-3-opus" || m.Contains("opus")) return "claude-3-opus-20240229";
        return model.Trim();
    }

    public static List<object> BuildAnthropicMessages(List<Message> history, string userMessage, List<string>? ragContext)
    {
        var fullUserMessage = AiPromptHelper.BuildPromptWithRag(userMessage, ragContext);
        var rawPast = history
            .Where(m => !m.IsDeleted && !string.IsNullOrWhiteSpace(m.Content))
            .ToList();

        // If the last message in history is the user message that was just saved by ChatController, exclude it from past history
        if (rawPast.Count > 0 && rawPast.Last().Role == "user" && string.Equals(rawPast.Last().Content.Trim(), userMessage.Trim(), StringComparison.Ordinal))
        {
            rawPast.RemoveAt(rawPast.Count - 1);
        }

        var alternating = new List<(string Role, string Content)>();

        // Anthropic requires:
        // 1. Roles must be 'user' or 'assistant'
        // 2. Roles must strictly alternate
        // 3. First message must be 'user'
        foreach (var msg in rawPast.TakeLast(10))
        {
            var role = string.Equals(msg.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            var content = msg.Content.Trim();

            // First message in Anthropic MUST be user
            if (alternating.Count == 0 && role != "user")
            {
                continue;
            }

            // If consecutive same role, combine them to guarantee strict alternation
            if (alternating.Count > 0 && alternating.Last().Role == role)
            {
                var prev = alternating.Last();
                alternating[^1] = (role, $"{prev.Content}\n\n{content}");
            }
            else
            {
                alternating.Add((role, content));
            }
        }

        // If history currently ends with a 'user' message, remove it because we are about to append fullUserMessage as 'user'
        if (alternating.Count > 0 && alternating.Last().Role == "user")
        {
            alternating.RemoveAt(alternating.Count - 1);
        }

        var result = new List<object>();
        foreach (var item in alternating)
        {
            result.Add(new { role = item.Role, content = item.Content });
        }

        // Final message is always the current user prompt with any RAG context
        result.Add(new { role = "user", content = fullUserMessage });

        return result;
    }
}
