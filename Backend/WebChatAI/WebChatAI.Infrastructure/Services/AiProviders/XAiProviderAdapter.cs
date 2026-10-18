using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class XAiProviderAdapter : IAiProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XAiProviderAdapter> _logger;

    public string Provider => "xai";
    public string DisplayName => "xAI Grok";
    public string DocUrl => "https://console.x.ai/";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("grok-2-latest", "Grok 2 (Latest)", "xai", "xAI'nin en güncel ve yetenekli amiral gemisi akıl yürütme modeli.", true, 131072),
        new("grok-2-vision-latest", "Grok 2 Vision (Latest)", "xai", "Gelişmiş görsel ve metinsel anlama kapasiteli Grok 2 modeli.", false, 32768),
        new("grok-beta", "Grok Beta", "xai", "Hızlı ve esnek akıl yürütme modeli.", false, 131072)
    };

    public XAiProviderAdapter(HttpClient httpClient, ILogger<XAiProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.ai/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fetchedModels = ParseXAiModels(content);
                return new AiKeyTestResponseDto(true, "xAI Grok API anahtarı başarıyla doğrulandı.", fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz xAI Grok API anahtarı (401 Unauthorized). Lütfen console.x.ai anahtarınızı kontrol edin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "xAI Grok API hız veya bakiye sınırına ulaşıldı (HTTP 429).");
            }

            return new AiKeyTestResponseDto(false, $"xAI API hatası: {response.StatusCode} - {errBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "xAI test connection failed");
            return new AiKeyTestResponseDto(false, $"xAI API bağlantısı başarısız: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.ai/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseXAiModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dynamically fetch xAI models, using defaults");
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
        var targetModel = string.IsNullOrWhiteSpace(model) ? "grok-2-latest" : model;
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var requestBody = new
        {
            model = targetModel,
            messages = messagesPayload,
            stream = true,
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.ai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                yield return "Hata: xAI Grok API anahtarınız geçersiz (HTTP 401). Lütfen hesap profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if ((int)response.StatusCode == 429)
            {
                yield return "Hata: xAI Grok API kotanız veya hız limitiniz doldu (HTTP 429). Lütfen console.x.ai hesabınızı kontrol edin veya başka bir model seçin.";
                yield break;
            }

            yield return $"xAI Grok API Hatası ({response.StatusCode}): {errText}";
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
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            deltaText = contentProp.GetString();
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

    private static List<object> BuildMessagesPayload(List<Message> history, string userMessage, List<string>? ragContext)
    {
        var list = new List<object>
        {
            new { role = "system", content = AiPromptHelper.SystemPrompt }
        };

        foreach (var msg in history.Where(m => !m.IsDeleted).TakeLast(10))
        {
            list.Add(new { role = msg.Role, content = msg.Content });
        }

        var fullUserMessage = AiPromptHelper.BuildPromptWithRag(userMessage, ragContext);
        list.Add(new { role = "user", content = fullUserMessage });

        return list;
    }

    private static List<AiModelInfoDto> ParseXAiModels(string json)
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
                    if (!string.IsNullOrEmpty(id) && id.Contains("grok"))
                    {
                        result.Add(new AiModelInfoDto(id, id, "xai", $"xAI {id} modeli"));
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
}
