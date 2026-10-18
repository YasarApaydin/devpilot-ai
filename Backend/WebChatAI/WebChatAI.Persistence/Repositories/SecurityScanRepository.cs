using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class SecurityScanRepository : Repository<SecurityScan>, ISecurityScanRepository
{
    public SecurityScanRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SecurityScan?> GetScanWithDetailsAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Id == scanId && s.UserId == userId && !s.IsDeleted)
            .Include(s => s.Consent)
            .Include(s => s.Findings.Where(f => !f.IsDeleted).OrderByDescending(f => f.CvssScore ?? 0))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<SecurityScan>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await _dbSet
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveRunningScanAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(s => s.UserId == userId && !s.IsDeleted && (s.Status == "Running" || s.Status == "Pending"), cancellationToken);
    }
}
