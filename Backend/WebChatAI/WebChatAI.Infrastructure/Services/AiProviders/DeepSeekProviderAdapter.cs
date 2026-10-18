using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class DeepSeekProviderAdapter : IAiProviderAdapter
{
    private const string BaseUrl = "https://api.deepseek.com";
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekProviderAdapter> _logger;

    public string Provider => "deepseek";
    public string DisplayName => "DeepSeek";
    public string DocUrl => "https://platform.deepseek.com/api_keys";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("deepseek-chat", "DeepSeek V3 Chat", "deepseek", "Hızlı, dengeli ve yüksek kapasiteli genel sohbet ve kodlama modeli (DeepSeek-V3).", true, 163840),
        new("deepseek-reasoner", "DeepSeek R1 (Reasoner)", "deepseek", "İleri düzey matematik, algoritma ve derin teknik muhakeme için düşünme (CoT) modeli.", false, 163840),
        new("deepseek-flash", "DeepSeek V4.1 Flash", "deepseek", "1M bağlam pencereli, hızlı ve maliyet odaklı yeni nesil DeepSeek Flash modeli.", false, 1048576),
        new("deepseek-v4-pro", "DeepSeek V4 Pro", "deepseek", "Karmaşık yazılım mühendisliği ve derin akıl yürütme için MoE amiral gemisi model.", false, 1048576)
    };

    public DeepSeekProviderAdapter(HttpClient httpClient, ILogger<DeepSeekProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiKeyTestResponseDto(false, "DeepSeek API anahtarı boş olamaz.");
        }

        try
        {
            var cleanKey = apiKey.Trim();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fetchedModels = ParseDeepSeekModels(content);
                return new AiKeyTestResponseDto(
                    true,
                    "DeepSeek API anahtarı başarıyla doğrulandı.",
                    fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz DeepSeek API anahtarı (401 Unauthorized). Lütfen platform.deepseek.com üzerinden aldığınız anahtarı kontrol edin.");
            }
            if ((int)response.StatusCode == 402)
            {
                return new AiKeyTestResponseDto(false, "DeepSeek API hesabınızda yeterli bakiye bulunmuyor (HTTP 402 Insufficient Balance). Lütfen platform.deepseek.com/top_up sayfasından bakiye yükleyin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "DeepSeek API hız veya kota sınırına ulaşıldı (HTTP 429). Lütfen birkaç saniye sonra tekrar deneyin.");
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AiKeyTestResponseDto(false, $"DeepSeek API bağlantı hatası ({(int)response.StatusCode}): {SanitizeErrorBody(errBody, cleanKey)}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AiKeyTestResponseDto(false, "DeepSeek API sunucusu zaman aşımına uğradı (Timeout).");
        }
        catch (Exception ex)
        {
            _logger.LogError("DeepSeek test connection failed: {ErrorType}", ex.GetType().Name);
            return new AiKeyTestResponseDto(false, $"DeepSeek API bağlantısı kurulamadı: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return DefaultModels.ToList();

        try
        {
            var cleanKey = apiKey.Trim();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseDeepSeekModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to dynamically fetch DeepSeek models ({ErrorType}), using defaults", ex.GetType().Name);
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
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return "Hata: DeepSeek API anahtarı eksik. Lütfen profilinizden API anahtarınızı kaydedin.";
            yield break;
        }

        var cleanKey = apiKey.Trim();
        var targetModel = NormalizeDeepSeekModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var (response, statusCode, errorText) = await SendChatRequestAsync(targetModel, cleanKey, messagesPayload, cancellationToken);

        // If account does not yet have access to deepseek-v4-pro or deepseek-flash (404 / Model Not Exist), fallback once to deepseek-chat
        if (response == null &&
            (statusCode == 400 || statusCode == 404) &&
            errorText != null &&
            (errorText.Contains("Model Not Exist", StringComparison.OrdinalIgnoreCase) ||
             errorText.Contains("model_not_found", StringComparison.OrdinalIgnoreCase) ||
             errorText.Contains("does not exist", StringComparison.OrdinalIgnoreCase)) &&
            !string.Equals(targetModel, "deepseek-chat", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("DeepSeek model {Model} not available on account; falling back to deepseek-chat.", targetModel);
            (response, statusCode, errorText) = await SendChatRequestAsync("deepseek-chat", cleanKey, messagesPayload, cancellationToken);
        }

        if (response == null)
        {
            if (statusCode == 401 || statusCode == 403)
            {
                yield return "Hata: DeepSeek API anahtarınız geçersiz veya yetkisiz (HTTP 401). Lütfen profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if (statusCode == 402)
            {
                yield return "Hata: DeepSeek API hesabınızda yeterli bakiye bulunmuyor (HTTP 402). Lütfen platform.deepseek.com hesabınıza bakiye ekleyin veya başka bir model seçin.";
                yield break;
            }
            if (statusCode == 429)
            {
                yield return "Hata: DeepSeek API hız veya kota sınırına ulaşıldı (HTTP 429). Lütfen biraz bekleyip tekrar deneyin veya başka bir sağlayıcı seçin.";
                yield break;
            }
            if (statusCode == 408)
            {
                yield return "Hata: DeepSeek API sunucusu yanıt vermedi (zaman aşımı / 408).";
                yield break;
            }
            if (statusCode == 404 || (errorText != null && errorText.Contains("Model Not Exist", StringComparison.OrdinalIgnoreCase)))
            {
                yield return $"Hata: Seçilen DeepSeek modeli ('{targetModel}') bulunamadı (HTTP 404).";
                yield break;
            }

            yield return $"DeepSeek API Hatası ({statusCode}): {SanitizeErrorBody(errorText, cleanKey)}";
            yield break;
        }

        using (response)
        using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
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
                        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                            {
                                deltaText = contentProp.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // Ignore partial SSE JSON chunk errors
                    }

                    if (!string.IsNullOrEmpty(deltaText))
                    {
                        yield return deltaText;
                    }
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

    private async Task<(HttpResponseMessage? Response, int StatusCode, string? ErrorText)> SendChatRequestAsync(
        string targetModel,
        string cleanKey,
        List<object> messagesPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            var isReasoner = targetModel.Contains("reasoner", StringComparison.OrdinalIgnoreCase) ||
                             targetModel.Contains("r1", StringComparison.OrdinalIgnoreCase);

            object requestBody = isReasoner
                ? new
                {
                    model = targetModel,
                    messages = messagesPayload,
                    stream = true
                }
                : new
                {
                    model = targetModel,
                    messages = messagesPayload,
                    stream = true,
                    temperature = 0.7
                };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (response, (int)response.StatusCode, null);
            }

            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;
            response.Dispose();
            return (null, statusCode, errText);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, 408, "DeepSeek sunucusu zaman aşımına uğradı.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, 500, ex.Message);
        }
    }

    public static string NormalizeDeepSeekModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "deepseek-chat";

        var trimmed = model.Trim();
        var lower = trimmed.ToLowerInvariant();

        if (lower.StartsWith("deepseek/"))
            trimmed = trimmed["deepseek/".Length..];

        lower = trimmed.ToLowerInvariant();

        if (lower == "deepseek-v4.1-flash" || lower == "deepseek-v4-flash")
            return "deepseek-flash";

        if (lower == "deepseek-r1" || lower == "r1")
            return "deepseek-reasoner";

        if (lower == "deepseek-v3" || lower == "v3")
            return "deepseek-chat";

        return trimmed;
    }

    private static List<object> BuildMessagesPayload(List<Message> history, string userMessage, List<string>? ragContext)
    {
        var list = new List<object>
        {
            new { role = "system", content = AiPromptHelper.SystemPrompt }
        };

        var pastMessages = history.Where(m => !m.IsDeleted && !string.IsNullOrWhiteSpace(m.Content)).ToList();
        if (pastMessages.Count > 0 &&
            pastMessages[^1].Role == "user" &&
            string.Equals(pastMessages[^1].Content.Trim(), userMessage.Trim(), StringComparison.Ordinal))
        {
            pastMessages.RemoveAt(pastMessages.Count - 1);
        }

        foreach (var msg in pastMessages.TakeLast(10))
        {
            list.Add(new { role = msg.Role, content = msg.Content });
        }

        var fullUserMessage = AiPromptHelper.BuildPromptWithRag(userMessage, ragContext);
        list.Add(new { role = "user", content = fullUserMessage });

        return list;
    }

    private List<AiModelInfoDto> ParseDeepSeekModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                var knownMap = DefaultModels.ToDictionary(m => m.Id, m => m, StringComparer.OrdinalIgnoreCase);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var el in data.EnumerateArray())
                {
                    if (!el.TryGetProperty("id", out var idProp)) continue;
                    var id = idProp.GetString();
                    if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) continue;

                    if (knownMap.TryGetValue(id, out var known))
                    {
                        result.Add(known);
                    }
                    else
                    {
                        result.Add(new AiModelInfoDto(
                            id,
                            FormatDisplayName(id),
                            "deepseek",
                            $"DeepSeek resmi API modeli ({id}).",
                            false,
                            163840));
                    }
                }

                // Ensure standard catalog models are also selectable if not returned in minimal list
                foreach (var def in DefaultModels)
                {
                    if (seen.Add(def.Id))
                    {
                        result.Add(def);
                    }
                }
            }
        }
        catch
        {
            // fallback to defaults
        }

        return result.Count > 0 ? result : DefaultModels.ToList();
    }

    private static string FormatDisplayName(string id) => id.ToLowerInvariant() switch
    {
        "deepseek-chat" => "DeepSeek V3 Chat",
        "deepseek-reasoner" => "DeepSeek R1 (Reasoner)",
        "deepseek-flash" => "DeepSeek V4.1 Flash",
        "deepseek-v4-pro" => "DeepSeek V4 Pro",
        _ => id
    };

    private static string SanitizeErrorBody(string? errorBody, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(errorBody)) return "Bilinmeyen sağlayıcı hatası.";
        var sanitized = !string.IsNullOrWhiteSpace(apiKey)
            ? errorBody.Replace(apiKey, "***", StringComparison.OrdinalIgnoreCase)
            : errorBody;
        return sanitized.Length > 300 ? sanitized[..300] + "..." : sanitized;
    }
}
