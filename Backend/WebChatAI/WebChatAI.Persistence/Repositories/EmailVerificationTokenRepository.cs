using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class EmailVerificationTokenRepository : Repository<EmailVerificationToken>, IEmailVerificationTokenRepository
{
    public EmailVerificationTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<EmailVerificationToken?> GetLatestActiveTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetByCodeHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId && t.CodeHash == codeHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existingTokens = await _dbSet
            .Where(t => t.UserId == userId && t.UsedAt == null && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var token in existingTokens)
        {
            token.IsDeleted = true;
            token.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(token);
        }
    }
}
