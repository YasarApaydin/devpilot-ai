using WebChatAI.Application.DTOs;

namespace WebChatAI.Application.Interfaces;

public interface IAiProviderRegistry
{
    IAiProviderAdapter? GetAdapter(string provider);
    IReadOnlyList<IAiProviderAdapter> GetAllAdapters();
}
