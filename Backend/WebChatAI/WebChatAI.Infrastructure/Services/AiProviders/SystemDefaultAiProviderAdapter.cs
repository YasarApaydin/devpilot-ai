using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class SystemDefaultAiProviderAdapter : IAiProviderAdapter
{
    private readonly IAiService _aiService;

    public string Provider => "system";
    public string DisplayName => "DevPilot Default (Sistem)";
    public string DocUrl => "";

    public IReadOnlyList<AiModelInfoDto> DefaultModels => new List<AiModelInfoDto>
    {
        new("default", "DevPilot Default (Qwen 3.8 / Groq)", "system", "DevPilot AI dahili yüksek hızlı LPU akıl yürütme ve Türkçe kodlama motoru. Ek API anahtarı gerektirmez.", true, 131072)
    };

    public SystemDefaultAiProviderAdapter(IAiService aiService)
    {
        _aiService = aiService;
    }

    public Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AiKeyTestResponseDto(true, "DevPilot AI dahili sistemi her zaman etkindir.", DefaultModels.ToList()));
    }

    public Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DefaultModels.ToList());
    }

    public IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default)
    {
        return _aiService.StreamChatAsync(history, userMessage, null, ragContext, cancellationToken);
    }

    public Task<string> GenerateChatResponseAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default)
    {
        return _aiService.GenerateChatResponseAsync(history, userMessage, null, ragContext, cancellationToken);
    }
}
