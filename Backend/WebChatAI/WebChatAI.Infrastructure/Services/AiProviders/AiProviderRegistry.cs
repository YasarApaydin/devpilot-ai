using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services.AiProviders;

public class AiProviderRegistry : IAiProviderRegistry
{
    private readonly Dictionary<string, IAiProviderAdapter> _adapters;

    public AiProviderRegistry(IEnumerable<IAiProviderAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.Provider.ToLowerInvariant(), a => a, StringComparer.OrdinalIgnoreCase);
    }

    public IAiProviderAdapter? GetAdapter(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider)) return null;
        _adapters.TryGetValue(provider.Trim().ToLowerInvariant(), out var adapter);
        return adapter;
    }

    public IReadOnlyList<IAiProviderAdapter> GetAllAdapters()
    {
        return _adapters.Values.ToList();
    }
}
