using WebChatAI.Domain.Entities;

namespace WebChatAI.Application.Interfaces;

public interface IAiService
{
    IAsyncEnumerable<string> StreamChatAsync(
        List<Message> history,
        string userMessage,
        string? model = null,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default);

    Task<string> GenerateChatResponseAsync(
        List<Message> history,
        string userMessage,
        string? model = null,
        List<string>? ragContext = null,
        CancellationToken cancellationToken = default);
}
