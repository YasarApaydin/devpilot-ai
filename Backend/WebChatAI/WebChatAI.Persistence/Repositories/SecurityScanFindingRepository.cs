using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class SecurityScanFindingRepository : Repository<SecurityScanFinding>, ISecurityScanFindingRepository
{
    public SecurityScanFindingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<SecurityScanFinding>> GetByScanIdAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.SecurityScanId == scanId && !f.IsDeleted)
            .OrderByDescending(f => f.CvssScore ?? 0)
            .ToListAsync(cancellationToken);
    }
}
