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

public class MistralProviderAdapter : IAiProviderAdapter
{
    private const string BaseUrl = "https://api.mistral.ai/v1";
    private readonly HttpClient _httpClient;
    private readonly ILogger<MistralProviderAdapter> _logger;

    public string Provider => "mistral";
    public string DisplayName => "Mistral AI";
    public string DocUrl => "https://console.mistral.ai/api-keys";

    // Verified official Mistral API models (per docs.mistral.ai: devstral-2512 is retired on direct Mistral API and replaced by mistral-medium-3-5)
    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("mistral-medium-3-5", "Mistral Medium 3.5 (Devstral Halefi)", "mistral", "Ajan iş yükleri ve yazılım mühendisliği için optimize edilmiş sınır model (Emekliye ayrılan Devstral 2'nin resmi API halefi).", true, 262144),
        new("mistral-large-4", "Mistral Large 4", "mistral", "1M bağlam pencereli, 1.05T MoE mimarili en gelişmiş Mistral amiral gemisi model.", false, 1048576),
        new("codestral-2508", "Codestral 25.08", "mistral", "80+ programlama dilinde düşük gecikmeli kod üretimi, hata düzeltme ve test yazımı modeli.", false, 131072),
        new("mistral-small-2603", "Mistral Small 4", "mistral", "Talimat, akıl yürütme ve kodlamayı birleştiren yüksek verimli hibrit model.", false, 262144)
    };

    public MistralProviderAdapter(HttpClient httpClient, ILogger<MistralProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiKeyTestResponseDto(false, "Mistral API anahtarı boş olamaz.");
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
                var fetchedModels = ParseMistralModels(content);
                return new AiKeyTestResponseDto(
                    true,
                    "Mistral AI API anahtarı başarıyla doğrulandı.",
                    fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz Mistral AI API anahtarı (401 Unauthorized). Lütfen console.mistral.ai/api-keys üzerinden aldığınız anahtarı kontrol edin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "Mistral AI API hız veya kota sınırına ulaşıldı (HTTP 429). Lütfen console.mistral.ai çalışma alanı limitlerinizi kontrol edin.");
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AiKeyTestResponseDto(false, $"Mistral AI API bağlantı hatası ({(int)response.StatusCode}): {SanitizeErrorBody(errBody, cleanKey)}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AiKeyTestResponseDto(false, "Mistral AI API sunucusu zaman aşımına uğradı (Timeout).");
        }
        catch (Exception ex)
        {
            _logger.LogError("Mistral test connection failed: {ErrorType}", ex.GetType().Name);
            return new AiKeyTestResponseDto(false, $"Mistral AI API bağlantısı kurulamadı: {ex.Message}");
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
                var models = ParseMistralModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to dynamically fetch Mistral models ({ErrorType}), using defaults", ex.GetType().Name);
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
            yield return "Hata: Mistral AI API anahtarı eksik. Lütfen profilinizden API anahtarınızı kaydedin.";
            yield break;
        }

        var cleanKey = apiKey.Trim();
        var targetModel = NormalizeMistralModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var (response, statusCode, errorText) = await SendChatRequestAsync(targetModel, cleanKey, messagesPayload, cancellationToken);

        // If a specific versioned alias is not enabled on the user's tier, fallback once to mistral-large-latest
        if (response == null &&
            (statusCode == 400 || statusCode == 404) &&
            errorText != null &&
            (errorText.Contains("invalid_model", StringComparison.OrdinalIgnoreCase) ||
             errorText.Contains("model_not_found", StringComparison.OrdinalIgnoreCase) ||
             errorText.Contains("does not exist", StringComparison.OrdinalIgnoreCase)) &&
            !string.Equals(targetModel, "mistral-large-latest", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Mistral model {Model} returned {Status}; falling back to mistral-large-latest.", targetModel, statusCode);
            (response, statusCode, errorText) = await SendChatRequestAsync("mistral-large-latest", cleanKey, messagesPayload, cancellationToken);
        }

        if (response == null)
        {
            if (statusCode == 401 || statusCode == 403)
            {
                yield return "Hata: Mistral AI API anahtarınız geçersiz veya yetkisiz (HTTP 401). Lütfen profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if (statusCode == 429)
            {
                yield return "Hata: Mistral AI API hız veya kota sınırına ulaşıldı (HTTP 429). Lütfen console.mistral.ai hesabınızı kontrol edin veya başka bir model seçin.";
                yield break;
            }
            if (statusCode == 408)
            {
                yield return "Hata: Mistral AI API sunucusu yanıt vermedi (zaman aşımı / 408).";
                yield break;
            }
            if (statusCode == 404 || (errorText != null && errorText.Contains("invalid_model", StringComparison.OrdinalIgnoreCase)))
            {
                yield return $"Hata: Seçilen Mistral modeli ('{targetModel}') bulunamadı (HTTP 404).";
                yield break;
            }

            yield return $"Mistral AI API Hatası ({statusCode}): {SanitizeErrorBody(errorText, cleanKey)}";
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
            var requestBody = new
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
            return (null, 408, "Mistral AI sunucusu zaman aşımına uğradı.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, 500, ex.Message);
        }
    }

    public static string NormalizeMistralModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "mistral-medium-3-5";

        var trimmed = model.Trim();
        var lower = trimmed.ToLowerInvariant();

        if (lower.StartsWith("mistralai/"))
            trimmed = trimmed["mistralai/".Length..];

        lower = trimmed.ToLowerInvariant();

        // Devstral 2 (devstral-2512) and Devstral Small/Medium 2507 were retired on Mistral's direct API;
        // Official Mistral documentation designates Mistral Medium 3.5 (mistral-medium-3-5) as the active replacement.
        if (lower.Contains("devstral"))
            return "mistral-medium-3-5";

        if (lower == "mistral-large-4-0" || lower == "mistral-large" || lower == "mistral-large-latest")
            return "mistral-large-4";

        if (lower == "mistral-medium-3-5-26-04" || lower == "mistral-medium-2508" || lower == "mistral-medium-2505" || lower == "mistral-medium-latest")
            return "mistral-medium-3-5";

        if (lower == "mistral-small-4-0-26-03" || lower == "mistral-small-2506" || lower == "mistral-small-2503" || lower == "mistral-small-latest")
            return "mistral-small-2603";

        if (lower == "codestral-25-08" || lower == "codestral-2501" || lower == "codestral" || lower == "codestral-latest")
            return "codestral-2508";

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

    private List<AiModelInfoDto> ParseMistralModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                var availableIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in data.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            availableIds.Add(id);
                        }
                    }
                }

                foreach (var def in DefaultModels)
                {
                    if (availableIds.Contains(def.Id))
                    {
                        result.Add(def);
                    }
                }
            }
        }
        catch
        {
            // fallback
        }

        return result.Count > 0 ? result : DefaultModels.ToList();
    }

    private static string SanitizeErrorBody(string? errorBody, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(errorBody)) return "Bilinmeyen sağlayıcı hatası.";
        var sanitized = !string.IsNullOrWhiteSpace(apiKey)
            ? errorBody.Replace(apiKey, "***", StringComparison.OrdinalIgnoreCase)
            : errorBody;
        return sanitized.Length > 300 ? sanitized[..300] + "..." : sanitized;
    }
}
