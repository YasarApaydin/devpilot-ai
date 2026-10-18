using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class OpenAiProviderAdapter : IAiProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiProviderAdapter> _logger;

    public string Provider => "openai";
    public string DisplayName => "OpenAI";
    public string DocUrl => "https://platform.openai.com/api-keys";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("gpt-4o", "GPT-4o", "openai", "En gelişmiş, akıllı ve hızlı amiral gemisi çok modlu model.", true, 128000),
        new("gpt-4o-mini", "GPT-4o Mini", "openai", "Hızlı, ekonomik ve günlük görevler için optimize model.", false, 128000),
        new("o1", "o1", "openai", "Kompleks matematik, kodlama ve bilimsel akıl yürütme modeli.", false, 200000),
        new("o1-mini", "o1-mini", "openai", "Akıl yürütme yetenekli hızlı ve kompakt kodlama modeli.", false, 128000),
        new("o3-mini", "o3-mini", "openai", "Yeni nesil STEM, matematik ve üst düzey kodlama akıl yürütme modeli.", false, 200000),
        new("gpt-4-turbo", "GPT-4 Turbo", "openai", "Yüksek performanslı önceki nesil GPT-4 Turbo modeli.", false, 128000)
    };

    public OpenAiProviderAdapter(HttpClient httpClient, ILogger<OpenAiProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fetchedModels = ParseOpenAiModels(content);
                return new AiKeyTestResponseDto(true, "OpenAI API anahtarı başarıyla doğrulandı.", fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz OpenAI API anahtarı (401 Unauthorized). Lütfen anahtarınızı kontrol edin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "OpenAI API kota sınırına veya bakiye yetersizliğine ulaşıldı (HTTP 429). Lütfen platform.openai.com üzerinden bakiye ve kullanım limitinizi kontrol edin.");
            }

            return new AiKeyTestResponseDto(false, $"OpenAI API bağlantı hatası: {response.StatusCode} - {errBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI test connection failed");
            return new AiKeyTestResponseDto(false, $"Bağlantı kurulamadı: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseOpenAiModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dynamically fetch OpenAI models, using defaults");
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
        var targetModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o" : model;
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var isReasoningModel = targetModel.StartsWith("o1", StringComparison.OrdinalIgnoreCase) ||
                               targetModel.StartsWith("o3", StringComparison.OrdinalIgnoreCase);

        object requestBody = isReasoningModel
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                yield return "Hata: OpenAI API anahtarınız geçersiz veya yetkisiz (HTTP 401). Lütfen hesap profilinizden API anahtarınızı güncelleyin.";
                yield break;
            }
            if ((int)response.StatusCode == 429)
            {
                yield return "Hata: OpenAI API kotanız veya kullanım limitiniz doldu (HTTP 429). Lütfen platform.openai.com hesabınızı kontrol edin veya başka bir model seçin.";
                yield break;
            }

            yield return $"OpenAI API Hatası ({response.StatusCode}): {errText}";
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
                    // Ignore partial json chunk parse errors
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

    private static List<AiModelInfoDto> ParseOpenAiModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                var availableIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in data.EnumerateArray())
                {
                    if (el.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            availableIds.Add(id);
                        }
                    }
                }

                var candidateModels = new (string Id, string Name, string Desc, bool IsDef, int Ctx)[]
                {
                    ("gpt-4o", "GPT-4o", "En gelişmiş, akıllı ve hızlı amiral gemisi çok modlu model.", true, 128000),
                    ("gpt-4o-mini", "GPT-4o Mini", "Hızlı, ekonomik ve günlük görevler için optimize model.", false, 128000),
                    ("o1", "o1", "Kompleks matematik, kodlama ve bilimsel akıl yürütme modeli.", false, 200000),
                    ("o1-mini", "o1-mini", "Akıl yürütme yetenekli hızlı ve kompakt kodlama modeli.", false, 128000),
                    ("o3-mini", "o3-mini", "Yeni nesil STEM, matematik ve üst düzey kodlama akıl yürütme modeli.", false, 200000),
                    ("gpt-4-turbo", "GPT-4 Turbo", "Yüksek performanslı önceki nesil GPT-4 Turbo modeli.", false, 128000)
                };

                foreach (var candidate in candidateModels)
                {
                    if (availableIds.Contains(candidate.Id))
                    {
                        result.Add(new AiModelInfoDto(candidate.Id, candidate.Name, "openai", candidate.Desc, candidate.IsDef, candidate.Ctx));
                    }
                }
            }
        }
        catch
        {
            // fallback
        }

        if (result.Count == 0)
        {
            result.AddRange(new[]
            {
                new AiModelInfoDto("gpt-4o", "GPT-4o", "openai", "En gelişmiş, akıllı ve hızlı amiral gemisi çok modlu model.", true, 128000),
                new AiModelInfoDto("gpt-4o-mini", "GPT-4o Mini", "openai", "Hızlı, ekonomik ve günlük görevler için optimize model.", false, 128000),
                new AiModelInfoDto("o1", "o1", "openai", "Kompleks matematik, kodlama ve bilimsel akıl yürütme modeli.", false, 200000),
                new AiModelInfoDto("o1-mini", "o1-mini", "openai", "Akıl yürütme yetenekli hızlı ve kompakt kodlama modeli.", false, 128000),
                new AiModelInfoDto("o3-mini", "o3-mini", "openai", "Yeni nesil STEM, matematik ve üst düzey kodlama akıl yürütme modeli.", false, 200000),
                new AiModelInfoDto("gpt-4-turbo", "GPT-4 Turbo", "openai", "Yüksek performanslı önceki nesil GPT-4 Turbo modeli.", false, 128000)
            });
        }

        return result;
    }
}
