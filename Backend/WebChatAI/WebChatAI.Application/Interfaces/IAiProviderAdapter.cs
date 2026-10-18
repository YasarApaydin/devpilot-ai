using WebChatAI.Application.DTOs;
using WebChatAI.Domain.Entities;

namespace WebChatAI.Application.Interfaces;

public interface IAiProviderAdapter
{
    string Provider { get; }
    string DisplayName { get; }
    string DocUrl { get; }
    IReadOnlyList<AiModelInfoDto> DefaultModels { get; }

    Task<AiKeyTestResponseDto> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<List<AiModelInfoDto>> GetAvailableModelsAsync(string apiKey, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateChatResponseAsync(
        List<Message> history,
        string userMessage,
        string model,
        string apiKey,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default);
}
