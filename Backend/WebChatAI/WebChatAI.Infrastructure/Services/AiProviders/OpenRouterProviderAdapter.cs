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

public class OpenRouterProviderAdapter : IAiProviderAdapter
{
    private const string BaseUrl = "https://openrouter.ai/api/v1";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterProviderAdapter> _logger;

    public string Provider => "openrouter";
    public string DisplayName => "OpenRouter";
    public string DocUrl => "https://openrouter.ai/settings/keys";

    // Verified provider/model IDs directly from https://openrouter.ai/api/v1/models catalog
    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("deepseek/deepseek-v4.1-flash", "DeepSeek V4.1 Flash (OpenRouter)", "openrouter", "OpenRouter üzerinden 1M bağlam pencereli yüksek hızlı DeepSeek V4.1 Flash modeli.", true, 1048576),
        new("deepseek/deepseek-v4-pro", "DeepSeek V4 Pro (OpenRouter)", "openrouter", "Karmaşık yazılım mühendisliği ve derin akıl yürütme için DeepSeek V4 Pro.", false, 1048576),
        new("deepseek/deepseek-chat", "DeepSeek V3 Chat (OpenRouter)", "openrouter", "Genel sohbet ve kodlama için kararlı DeepSeek V3 modeli.", false, 163840),
        new("deepseek/deepseek-r1", "DeepSeek R1 Reasoner (OpenRouter)", "openrouter", "Derin zincirleme düşünme (CoT) ve matematiksel akıl yürütme modeli.", false, 64000),
        new("mistralai/devstral-2512", "Mistral Devstral 2 (OpenRouter)", "openrouter", "Yazılım mühendisliği, çoklu dosya analizi ve kod ajanları için özelleştirilmiş Devstral 2 modeli.", false, 262144),
        new("mistralai/mistral-large-4-0", "Mistral Large 4 (OpenRouter)", "openrouter", "Mistral'in 1M bağlam pencereli en üst düzey çok modlu MoE amiral gemisi.", false, 1048576),
        new("mistralai/mistral-medium-3-5", "Mistral Medium 3.5 (OpenRouter)", "openrouter", "Ajan iş yükleri ve yazılım geliştirme için optimize edilmiş sınır model.", false, 262144),
        new("google/gemma-4-31b-it:free", "Google Gemma 4 31B IT (Ücretsiz)", "openrouter", "Google'ın açık ağırlıklı Gemma 4 31B modeli (OpenRouter ücretsiz katman).", false, 262144),
        new("google/gemma-4-31b-it", "Google Gemma 4 31B IT (OpenRouter)", "openrouter", "Google Gemma 4 31B Instruction-Tuned açık ağırlıklı model.", false, 262144),
        new("google/gemma-4-26b-a4b-it:free", "Google Gemma 4 26B MoE (Ücretsiz)", "openrouter", "Google Gemma 4 26B A4B MoE açık ağırlıklı model (OpenRouter ücretsiz katman).", false, 262144),
        new("google/gemma-3-27b-it", "Google Gemma 3 27B IT (OpenRouter)", "openrouter", "Google Gemma 3 27B Instruction-Tuned açık ağırlıklı model.", false, 131072)
    };

    public OpenRouterProviderAdapter(HttpClient httpClient, ILogger<OpenRouterProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiKeyTestResponseDto(false, "OpenRouter API anahtarı boş olamaz.");
        }

        try
        {
            var cleanKey = apiKey.Trim();

            // /api/v1/models is public on OpenRouter, so we verify key authenticity via /api/v1/auth/key
            using var authRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/auth/key");
            ApplyOpenRouterHeaders(authRequest, cleanKey);

            using var authResponse = await _httpClient.SendAsync(authRequest, cancellationToken);
            if (authResponse.IsSuccessStatusCode)
            {
                var models = await GetAvailableModelsAsync(cleanKey, cancellationToken);
                return new AiKeyTestResponseDto(
                    true,
                    "OpenRouter API anahtarı başarıyla doğrulandı.",
                    models.Count > 0 ? models : DefaultModels.ToList());
            }

            if (authResponse.StatusCode == HttpStatusCode.Unauthorized || authResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz OpenRouter API anahtarı (401 Unauthorized). Lütfen openrouter.ai/settings/keys üzerinden aldığınız 'sk-or-v1-...' anahtarınızı kontrol edin.");
            }
            if ((int)authResponse.StatusCode == 402)
            {
                return new AiKeyTestResponseDto(false, "OpenRouter hesabınızda yeterli kredi bulunmuyor (HTTP 402). Lütfen openrouter.ai/settings/credits sayfasından bakiyenizi kontrol edin.");
            }
            if ((int)authResponse.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "OpenRouter API hız veya kota sınırına ulaşıldı (HTTP 429).");
            }

            var errBody = await authResponse.Content.ReadAsStringAsync(cancellationToken);
            return new AiKeyTestResponseDto(false, $"OpenRouter API bağlantı hatası ({(int)authResponse.StatusCode}): {SanitizeErrorBody(errBody, cleanKey)}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new AiKeyTestResponseDto(false, "OpenRouter API sunucusu zaman aşımına uğradı (Timeout).");
        }
        catch (Exception ex)
        {
            _logger.LogError("OpenRouter test connection failed: {ErrorType}", ex.GetType().Name);
            return new AiKeyTestResponseDto(false, $"OpenRouter API bağlantısı kurulamadı: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanKey = apiKey?.Trim() ?? string.Empty;
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models");
            if (!string.IsNullOrEmpty(cleanKey))
            {
                ApplyOpenRouterHeaders(request, cleanKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseOpenRouterModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to dynamically fetch OpenRouter models ({ErrorType}), using defaults", ex.GetType().Name);
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
            yield return "Hata: OpenRouter API anahtarı eksik. Lütfen profilinizden API anahtarınızı kaydedin.";
            yield break;
        }

        var cleanKey = apiKey.Trim();
        var targetModel = NormalizeOpenRouterModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var (response, statusCode, errorText) = await SendChatRequestAsync(targetModel, cleanKey, messagesPayload, cancellationToken);

        // If a `:free` tier model hits rate limit (429) or capacity (503), try the non-free or default flash model once if different
        if (response == null &&
            (statusCode == 404 || statusCode == 400) &&
            errorText != null &&
            (errorText.Contains("No endpoints found", StringComparison.OrdinalIgnoreCase) ||
             errorText.Contains("not a valid model", StringComparison.OrdinalIgnoreCase)) &&
            !string.Equals(targetModel, "deepseek/deepseek-v4.1-flash", StringComparison.OrdinalIgnoreCase))
        {
            if (targetModel.EndsWith(":free", StringComparison.OrdinalIgnoreCase))
            {
                var paidEquivalent = targetModel[..^":free".Length];
                _logger.LogInformation("OpenRouter free model {Model} unavailable; trying {Fallback}.", targetModel, paidEquivalent);
                (response, statusCode, errorText) = await SendChatRequestAsync(paidEquivalent, cleanKey, messagesPayload, cancellationToken);
            }
        }

        if (response == null)
        {
            if (statusCode == 401 || statusCode == 403)
            {
                yield return "Hata: OpenRouter API anahtarınız geçersiz veya yetkisiz (HTTP 401/403). Lütfen profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if (statusCode == 402)
            {
                yield return "Hata: OpenRouter hesabınızda seçtiğiniz ücretli model için yeterli kredi bulunmuyor (HTTP 402). Lütfen openrouter.ai/settings/credits üzerinden bakiye ekleyin veya ücretsiz (:free) bir model seçin.";
                yield break;
            }
            if (statusCode == 429)
            {
                yield return "Hata: OpenRouter API hız veya kota sınırına ulaşıldı (HTTP 429). Lütfen birkaç saniye sonra tekrar deneyin veya başka bir model seçin.";
                yield break;
            }
            if (statusCode == 408)
            {
                yield return "Hata: OpenRouter API sunucusu yanıt vermedi (zaman aşımı / 408).";
                yield break;
            }
            if (statusCode == 404 || (errorText != null && errorText.Contains("not a valid model", StringComparison.OrdinalIgnoreCase)))
            {
                yield return $"Hata: Seçilen OpenRouter modeli ('{targetModel}') katalogda bulunamadı (HTTP 404).";
                yield break;
            }

            yield return $"OpenRouter API Hatası ({statusCode}): {SanitizeErrorBody(errorText, cleanKey)}";
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

                // OpenRouter sends ": OPENROUTER PROCESSING" SSE keep-alive comments; ignore lines starting with ':'
                if (line.StartsWith(':')) continue;

                if (line.StartsWith("data: "))
                {
                    var data = line["data: ".Length..].Trim();
                    if (data == "[DONE]") break;

                    string? deltaText = null;
                    string? inlineError = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        if (doc.RootElement.TryGetProperty("error", out var errObj) &&
                            errObj.TryGetProperty("message", out var errMsgProp))
                        {
                            inlineError = errMsgProp.GetString();
                        }
                        else if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
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

                    if (!string.IsNullOrEmpty(inlineError))
                    {
                        yield return $"Hata: OpenRouter akış hatası: {SanitizeErrorBody(inlineError, cleanKey)}";
                        yield break;
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
            ApplyOpenRouterHeaders(request, cleanKey);
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
            return (null, 408, "OpenRouter sunucusu zaman aşımına uğradı.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, 500, ex.Message);
        }
    }

    public static void ApplyOpenRouterHeaders(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://devpilot.ai");
        request.Headers.TryAddWithoutValidation("X-Title", "DevPilot AI");
    }

    public static string NormalizeOpenRouterModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "deepseek/deepseek-v4.1-flash";

        var trimmed = model.Trim();
        var lower = trimmed.ToLowerInvariant();

        return lower switch
        {
            "deepseek-flash" or "deepseek-v4.1-flash" => "deepseek/deepseek-v4.1-flash",
            "deepseek-v4-pro" => "deepseek/deepseek-v4-pro",
            "deepseek-chat" => "deepseek/deepseek-chat",
            "deepseek-r1" or "deepseek-reasoner" => "deepseek/deepseek-r1",
            "devstral" or "devstral-2" or "devstral-2512" => "mistralai/devstral-2512",
            "gemma-4-free" or "gemma-4-31b-it:free" => "google/gemma-4-31b-it:free",
            "gemma-4-31b-it" => "google/gemma-4-31b-it",
            "gemma-4-26b-a4b-it" => "google/gemma-4-26b-a4b-it",
            "gemma-3-27b-it" => "google/gemma-3-27b-it",
            _ => trimmed
        };
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

    private List<AiModelInfoDto> ParseOpenRouterModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                var catalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in data.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            catalogIds.Add(id);
                        }
                    }
                }

                // Filter DefaultModels to those confirmed in the live OpenRouter catalog
                foreach (var def in DefaultModels)
                {
                    if (catalogIds.Contains(def.Id))
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
