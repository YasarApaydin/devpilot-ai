using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class GeminiProviderAdapter : IAiProviderAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiProviderAdapter> _logger;

    public string Provider => "gemini";
    public string DisplayName => "Google Gemini";
    public string DocUrl => "https://aistudio.google.com/app/apikey";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("gemini-2.5-flash", "Gemini 2.5 Flash", "gemini", "Google hibrit akıl yürütme (thinking) modeli. Hızlı, kararlı ve önerilen varsayılan.", true, 1048576),
        new("gemini-3.8-flash", "Gemini 3.8 Flash", "gemini", "Yeni nesil ultra hızlı multimodal ve kod analizi modeli (1M bağlam).", false, 1048576),
        new("gemini-3.5-flash", "Gemini 3.5 Flash", "gemini", "Yüksek hız ve gelişmiş muhakeme dengesi sunan güncel Gemini modeli.", false, 1048576),
        new("gemini-3.1-pro-preview", "Gemini 3.1 Pro", "gemini", "Karmaşık yazılım mimarisi ve derin muhakeme için Google amiral gemisi model.", false, 1048576),
        new("gemini-2.5-pro", "Gemini 2.5 Pro", "gemini", "Karmaşık kodlama ve derin muhakeme için kararlı Pro model.", false, 1048576),
        new("gemini-2.0-flash", "Gemini 2.0 Flash", "gemini", "Ultra hızlı, düşük gecikmeli multimodal model.", false, 1048576),
        new("gemma-4-31b-it", "Gemma 4 31B Instruct (Açık Ağırlıklı)", "gemini", "Google Gemma 4 31B açık ağırlıklı (open-weight) yoğun model — Gemini API üzerinden ücretsiz/doğrudan erişim.", false, 131072),
        new("gemma-4-26b-a4b-it", "Gemma 4 26B MoE Instruct (Açık Ağırlıklı)", "gemini", "Google Gemma 4 26B (4B aktif) MoE açık ağırlıklı hızlı model — Gemini API üzerinden erişim.", false, 131072),
        new("gemma-3-27b-it", "Gemma 3 27B Instruct (Açık Ağırlıklı)", "gemini", "Google Gemma 3 27B açık ağırlıklı (open-weight) talimat modeli.", false, 131072)
    };

    public GeminiProviderAdapter(HttpClient httpClient, ILogger<GeminiProviderAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanKey = apiKey.Trim();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(cleanKey)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", cleanKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var fetchedModels = ParseGeminiModels(content);
                return new AiKeyTestResponseDto(true, "Google Gemini API anahtarı başarıyla doğrulandı.", fetchedModels.Count > 0 ? fetchedModels : DefaultModels.ToList());
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // Fallback: test minimal generateContent with gemini-2.5-flash directly
            try
            {
                var generateUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={Uri.EscapeDataString(cleanKey)}";
                using var genReq = new HttpRequestMessage(HttpMethod.Post, generateUrl);
                genReq.Headers.TryAddWithoutValidation("x-goog-api-key", cleanKey);
                genReq.Content = new StringContent("{\"contents\":[{\"parts\":[{\"text\":\"ping\"}]}]}", Encoding.UTF8, "application/json");

                using var genResp = await _httpClient.SendAsync(genReq, cancellationToken);
                if (genResp.IsSuccessStatusCode)
                {
                    return new AiKeyTestResponseDto(true, "Google Gemini API anahtarı başarıyla doğrulandı.", DefaultModels.ToList());
                }

                var genErr = await genResp.Content.ReadAsStringAsync(cancellationToken);
                if (genResp.StatusCode == System.Net.HttpStatusCode.Unauthorized || genErr.Contains("API_KEY_INVALID"))
                {
                    return new AiKeyTestResponseDto(false, "Geçersiz Google Gemini API anahtarı. Lütfen aistudio.google.com üzerinden aldığınız anahtarı kontrol edin.");
                }
            }
            catch
            {
                // ignore fallback error
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || (int)response.StatusCode == 400 && errBody.Contains("API_KEY_INVALID"))
            {
                return new AiKeyTestResponseDto(false, "Geçersiz Google Gemini API anahtarı. Lütfen aistudio.google.com üzerinden aldığınız anahtarı kontrol edin.");
            }
            if ((int)response.StatusCode == 429)
            {
                return new AiKeyTestResponseDto(false, "Google Gemini API kota veya istek sınırına ulaşıldı (HTTP 429).");
            }

            return new AiKeyTestResponseDto(false, $"Gemini API yanıt hatası: {response.StatusCode} - {errBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini test connection failed");
            return new AiKeyTestResponseDto(false, $"Gemini API bağlantısı başarısız: {ex.Message}");
        }
    }

    public async Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanKey = apiKey.Trim();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(cleanKey)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", cleanKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var models = ParseGeminiModels(content);
                if (models.Count > 0) return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Gemini models dynamically, using defaults");
        }

        return DefaultModels.ToList();
    }

    private async Task<(HttpResponseMessage? Response, int StatusCode, string? Error)> SendGeminiRequestAsync(
        string currentModel,
        string cleanKey,
        List<Message> history,
        string userMessage,
        List<string>? ragContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:streamGenerateContent?alt=sse&key={Uri.EscapeDataString(cleanKey)}";

            var contents = new List<object>();
            foreach (var msg in history.Where(m => !m.IsDeleted).TakeLast(10))
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new
                {
                    role = role,
                    parts = new[] { new { text = msg.Content } }
                });
            }

            var isGemma = currentModel.StartsWith("gemma", StringComparison.OrdinalIgnoreCase);
            var fullUserMessage = AiPromptHelper.BuildPromptWithRag(userMessage, ragContext);

            if (isGemma)
            {
                fullUserMessage = $"[Sistem Yönergesi: {AiPromptHelper.SystemPrompt}]\n\n{fullUserMessage}";
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = fullUserMessage } }
            });

            object requestBody = isGemma
                ? new
                {
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.7
                    }
                }
                : new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = AiPromptHelper.SystemPrompt } }
                    },
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.7
                    }
                };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("x-goog-api-key", cleanKey);
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
            return (null, 408, "Google Gemini sunucusu yanıt vermedi (zaman aşımı).");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, 500, ex.Message);
        }
    }

    private static string? ParseCandidateText(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var part = parts[0];
                    if (part.TryGetProperty("text", out var textProp))
                    {
                        return textProp.GetString();
                    }
                }
            }
        }
        catch
        {
            // Ignore incomplete SSE json fragments
        }
        return null;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = NormalizeGeminiModel(model);
        var cleanKey = apiKey.Trim();

        // High-availability fallback chain for capacity shortages (503), rate-limits (429), or decommissioning (404/400)
        var modelsToTry = new List<string> { modelId };

        if (!modelsToTry.Contains("gemini-2.5-flash"))
            modelsToTry.Add("gemini-2.5-flash");
        if (!modelsToTry.Contains("gemini-2.0-flash"))
            modelsToTry.Add("gemini-2.0-flash");
        if (!modelsToTry.Contains("gemini-1.5-flash"))
            modelsToTry.Add("gemini-1.5-flash");

        string? lastError = null;
        int lastStatusCode = 0;

        foreach (var currentModel in modelsToTry)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            const int maxRetries = 2;
            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    // Exponential backoff with small random jitter (1.2s - 2.5s)
                    var delayMs = 1200 * attempt + Random.Shared.Next(100, 350);
                    _logger.LogInformation("Gemini model {Model} encountered status {Status}. Retrying attempt {Attempt} after {Delay}ms...", currentModel, lastStatusCode, attempt, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }

                var (response, status, error) = await SendGeminiRequestAsync(currentModel, cleanKey, history, userMessage, ragContext, cancellationToken);

                if (response != null)
                {
                    var streamedAny = false;
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

                                var chunkText = ParseCandidateText(data);
                                if (!string.IsNullOrEmpty(chunkText))
                                {
                                    streamedAny = true;
                                    yield return chunkText;
                                }
                            }
                        }
                    }

                    if (streamedAny)
                    {
                        yield break;
                    }
                }

                lastStatusCode = status;
                lastError = error;

                // If API key is invalid, terminate immediately without retrying
                if (lastStatusCode == 401 || (lastStatusCode == 400 && lastError != null && lastError.Contains("API_KEY_INVALID")))
                {
                    yield return "Hata: Google Gemini API anahtarınız geçersiz. Lütfen profilinizdeki 'AI Modellerim' sekmesinden aistudio.google.com üzerinden aldığınız anahtarı güncelleyin.";
                    yield break;
                }

                // Transient errors to retry: 503 (high demand / unavailable), 429 (rate-limit), 500, 502, 504
                var isTransient = lastStatusCode == 503 || lastStatusCode == 429 || lastStatusCode == 500 || lastStatusCode == 502 || lastStatusCode == 504 ||
                                  (lastError != null && (lastError.Contains("high demand") || lastError.Contains("UNAVAILABLE") || lastError.Contains("RESOURCE_EXHAUSTED")));

                if (isTransient && attempt < maxRetries)
                {
                    continue;
                }

                _logger.LogWarning("Gemini model {Model} failed with {Status}. Attempting fallback model...", currentModel, lastStatusCode);
                break;
            }
        }

        // If all retries and fallback models failed, provide friendly actionable guidance
        if (lastStatusCode == 503 || (lastError != null && (lastError.Contains("high demand") || lastError.Contains("UNAVAILABLE"))))
        {
            yield return "Bilgi: Google Gemini sunucuları şu anda dünya genelinde yüksek talep (HTTP 503 UNAVAILABLE) yaşamaktadır. Sistem otomatik olarak alternatif modelleri ve gecikmeli tekrar denemelerini gerçekleştirdi ancak Google tarafında yoğunluk sürüyor. Lütfen birkaç saniye sonra tekrar deneyin veya sohbet penceresinden DevPilot Default (Groq) veya OpenAI sağlayıcısını seçin.";
            yield break;
        }

        if (lastStatusCode == 429 || (lastError != null && lastError.Contains("RESOURCE_EXHAUSTED")))
        {
            yield return "Hata: Google Gemini API hız veya ücretsiz kota limitiniz doldu (HTTP 429). Google AI Studio ücretsiz planında dakikalık istek sınırı kısıtlıdır. Lütfen birkaç saniye bekleyip tekrar deneyin veya alternatif bir sağlayıcı seçin.";
            yield break;
        }

        yield return $"Google Gemini API Hatası ({lastStatusCode}): {lastError ?? "Bilinmeyen bir hata oluştu."}";
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

    public static string NormalizeGeminiModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return "gemini-3.8-flash";

        var m = model.Trim();
        if (m.StartsWith("models/"))
            m = m["models/".Length..];

        var lower = m.ToLowerInvariant().Replace(" ", "-");

        // Open-weight Gemma models served via Gemini API
        if (lower.Contains("gemma-4-31b") || lower == "gemma-4" || lower == "gemma4")
            return "gemma-4-31b-it";
        if (lower.Contains("gemma-4-26b"))
            return "gemma-4-26b-a4b-it";
        if (lower.Contains("gemma-3-27b") || lower == "gemma-3")
            return "gemma-3-27b-it";
        if (lower.StartsWith("gemma-"))
            return lower;

        if (lower.Contains("3.8"))
            return "gemini-3.8-flash";

        if (lower.Contains("3.5-flash-lite") || lower.Contains("3.5-lite"))
            return "gemini-3.5-flash-lite";

        if (lower.Contains("3.5-flash"))
            return "gemini-3.5-flash";

        if (lower.Contains("3.1-pro") || lower.Contains("3-pro"))
            return "gemini-3.1-pro-preview";

        if (lower.Contains("3.1-flash-lite") || lower.Contains("3.1-lite"))
            return "gemini-3.1-flash-lite-preview";

        if (lower.Contains("3-flash"))
            return "gemini-3-flash-preview";

        // Map legacy aliases (preserves unit test contract)
        if (lower.Contains("2.5-pro") || lower.Contains("1.5-pro") || lower == "gemini-pro")
            return "gemini-3.1-pro-preview";

        if (lower.Contains("2.5-flash") || lower.Contains("2.0-flash") || lower.Contains("1.5-flash") || lower == "gemini-flash")
            return "gemini-3.8-flash";

        return m;
    }

    private static List<AiModelInfoDto> ParseGeminiModels(string json)
    {
        var result = new List<AiModelInfoDto>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var el in models.EnumerateArray())
                {
                    var name = el.GetProperty("name").GetString() ?? string.Empty;
                    var modelId = name.StartsWith("models/") ? name["models/".Length..] : name;
                    var supportedMethods = el.TryGetProperty("supportedGenerationMethods", out var methods) ? methods.ToString() : "";

                    if (supportedMethods.Contains("generateContent") &&
                        (modelId.Contains("gemini") || modelId.Contains("gemma") || modelId.Contains("flash") || modelId.Contains("pro")))
                    {
                        var displayName = el.TryGetProperty("displayName", out var dn) ? dn.GetString() : modelId;
                        var isGemma = modelId.StartsWith("gemma", StringComparison.OrdinalIgnoreCase);
                        if (isGemma && displayName != null && !displayName.Contains("Açık Ağırlıklı"))
                        {
                            displayName = $"{displayName} (Açık Ağırlıklı)";
                        }
                        var desc = el.TryGetProperty("description", out var ds)
                            ? ds.GetString()
                            : isGemma
                                ? $"Google Gemma açık ağırlıklı (open-weight) {modelId} modeli"
                                : $"Google Gemini {modelId} modeli";

                        result.Add(new AiModelInfoDto(modelId, displayName ?? modelId, "gemini", desc ?? string.Empty, modelId == "gemini-2.5-flash"));
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
