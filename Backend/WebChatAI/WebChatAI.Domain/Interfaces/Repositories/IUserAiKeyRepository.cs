using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IUserAiKeyRepository : IRepository<UserAiKey>
{
    Task<UserAiKey?> GetByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<UserAiKey?> GetAnyByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
    Task<List<UserAiKey>> GetUserKeysAsync(Guid userId, CancellationToken cancellationToken = default);
}
