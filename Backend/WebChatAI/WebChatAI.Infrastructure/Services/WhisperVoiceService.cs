using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebChatAI.Application.Interfaces;
using WebChatAI.Infrastructure.Options;

namespace WebChatAI.Infrastructure.Services;

public class WhisperVoiceService : IVoiceService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<WhisperVoiceService> _logger;

    public WhisperVoiceService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        ILogger<WhisperVoiceService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAudioAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Groq API key is not set. Returning mock voice transcription.");
            return "DevPilot AI sesli komut sistemi test kaydı: Bu mesaj Whisper Large V3 Turbo ile başarıyla analiz edildi.";
        }

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(audioStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(_options.WhisperModel), "model");
        content.Add(new StringContent("tr"), "language"); // Turkish default

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/audio/transcriptions")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq Whisper API returned {Status}: {Error}", response.StatusCode, err);
                return "Ses kaydı analiz edilirken bir hata oluştu.";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("text", out var textElem))
            {
                return textElem.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Groq Whisper API");
            return "Ses kaydı işlenirken bir bağlantı hatası meydana geldi.";
        }
    }
}
