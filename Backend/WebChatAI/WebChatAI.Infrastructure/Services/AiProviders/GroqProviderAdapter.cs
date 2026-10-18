using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class GroqProviderAdapter : IAiProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqProviderAdapter> _logger;

    public string Provider => "groq";
    public string DisplayName => "GroqCloud";
    public string DocUrl => "https://console.groq.com/keys";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("openai/gpt-oss-120b", "GPT-OSS 120B", "groq", "Işık hızında LPU çıkarımı, güçlü genel zeka ve derin teknik muhakeme.", true, 131072),
        new("openai/gpt-oss-20b", "GPT-OSS 20B", "groq", "Ultra yüksek token/saniye hızında optimize verimli LPU çıkarımı.", false, 131072),
        new("llama-3.3-70b-versatile", "Llama 3.3 70B Versatile", "groq", "Meta amiral gemisi, yüksek kapasiteli ve çok amaçlı akıl yürütme modeli.", false, 128000)
    };

    public GroqProviderAdapter(HttpClient httpClient, ILogger<GroqProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fetchedModels = ParseGroqModels(content);
                return new AiKeyTestResponseDto(true, "GroqCloud API anahtarı başarıyla doğrulandı.", fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AiKeyTestResponseDto(false, "Geçersiz GroqCloud API anahtarı (401 Unauthorized). Lütfen console.groq.com anahtarınızı kontrol edin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "GroqCloud API hız veya kota sınırına ulaşıldı (HTTP 429).");
            }

            return new AiKeyTestResponseDto(false, $"GroqCloud API hatası: {response.StatusCode} - {errBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq test connection failed");
            return new AiKeyTestResponseDto(false, $"Groq API bağlantısı başarısız: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseGroqModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dynamically fetch Groq models, using defaults");
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
        var targetModel = NormalizeGroqModel(model);
        var messagesPayload = BuildMessagesPayload(history, userMessage, ragContext);

        var requestBody = new
        {
            model = targetModel,
            messages = messagesPayload,
            stream = true,
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync(cancellationToken);

            // Automatically recover if a deprecated or decommissioned model was passed
            if ((response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.NotFound) &&
                (errText.Contains("model_decommissioned") || errText.Contains("model_not_found") || errText.Contains("does not exist")) &&
                targetModel != "openai/gpt-oss-120b")
            {
                _logger.LogInformation("Model {TargetModel} failed on Groq. Automatically falling back to openai/gpt-oss-120b...", targetModel);
                await foreach (var chunk in StreamChatAsync(history, userMessage, "openai/gpt-oss-120b", apiKey, ragContext, cancellationToken))
                {
                    yield return chunk;
                }
                yield break;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                yield return "Hata: GroqCloud API anahtarınız geçersiz (HTTP 401). Lütfen hesap profilinizden anahtarınızı güncelleyin.";
                yield break;
            }
            if ((int)response.StatusCode == 429)
            {
                if (targetModel != "openai/gpt-oss-20b")
                {
                    _logger.LogInformation("Rate limit on Groq {TargetModel}. Falling back to openai/gpt-oss-20b...", targetModel);
                    await foreach (var chunk in StreamChatAsync(history, userMessage, "openai/gpt-oss-20b", apiKey, ragContext, cancellationToken))
                    {
                        yield return chunk;
                    }
                    yield break;
                }

                yield return "Hata: GroqCloud API hız sınırınız veya kotanız doldu (HTTP 429). Lütfen bir süre sonra tekrar deneyin veya başka bir model seçin.";
                yield break;
            }

            yield return $"GroqCloud API Hatası ({response.StatusCode}): {errText}";
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

    public static string NormalizeGroqModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "openai/gpt-oss-120b";

        var m = model.Trim().ToLowerInvariant();

        // Automatically map decommissioned/unavailable Groq models or Qwen (used as system default)
        if (m.Contains("qwen") || m.Contains("allam") || m.Contains("llama-3.3") || m.Contains("llama-3.1-70b") || m.Contains("deepseek") || m.Contains("mixtral") || m.Contains("llama3-70b"))
            return "openai/gpt-oss-120b";

        if (m.Contains("llama-3.1-8b") || m.Contains("llama3-8b") || m.Contains("gemma"))
            return "openai/gpt-oss-20b";

        return model.Trim();
    }

    private static List<AiModelInfoDto> ParseGroqModels(string json)
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
                    if (!string.IsNullOrEmpty(id) &&
                        !id.Contains("qwen", StringComparison.OrdinalIgnoreCase) &&
                        !id.Contains("whisper", StringComparison.OrdinalIgnoreCase) &&
                        !id.Contains("guard", StringComparison.OrdinalIgnoreCase) &&
                        !id.Contains("orpheus", StringComparison.OrdinalIgnoreCase))
                    {
                        var displayName = id switch
                        {
                            "openai/gpt-oss-120b" => "GPT-OSS 120B",
                            "openai/gpt-oss-20b" => "GPT-OSS 20B",
                            "allam-2-7b" => "ALLaM 2 7B",
                            _ => id
                        };

                        var desc = id switch
                        {
                            "openai/gpt-oss-120b" => "Işık hızında LPU çıkarımı, güçlü genel zeka, kodlama ve Türkçe muhakeme.",
                            "openai/gpt-oss-20b" => "Ultra yüksek token/saniye hızında optimize verimli LPU çıkarımı.",
                            "allam-2-7b" => "Hafif ve hızlı doğal dil sohbet modeli.",
                            _ => $"Groq {id} modeli"
                        };

                        result.Add(new AiModelInfoDto(id, displayName, "groq", desc, id == "openai/gpt-oss-120b"));
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
