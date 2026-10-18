using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class UserAiKeyRepository : Repository<UserAiKey>, IUserAiKeyRepository
{
    public UserAiKeyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserAiKey?> GetByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        return await _dbSet
            .Where(k => k.UserId == userId && k.Provider.ToLower() == normalizedProvider && !k.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserAiKey?> GetAnyByUserAndProviderAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(k => k.UserId == userId && k.Provider.ToLower() == normalizedProvider)
            .OrderByDescending(k => k.UpdatedAt ?? k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<UserAiKey>> GetUserKeysAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(k => k.UserId == userId && !k.IsDeleted)
            .OrderBy(k => k.Provider)
            .ToListAsync(cancellationToken);
    }
}
