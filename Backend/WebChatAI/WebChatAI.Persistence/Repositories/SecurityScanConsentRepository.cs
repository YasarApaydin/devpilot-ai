using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class SecurityScanConsentRepository : Repository<SecurityScanConsent>, ISecurityScanConsentRepository
{
    public SecurityScanConsentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SecurityScanConsent?> GetByScanIdAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.SecurityScanId == scanId && c.UserId == userId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
