using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class AbuseAuditLogRepository : Repository<AbuseAuditLog>, IAbuseAuditLogRepository
{
    public AbuseAuditLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<AbuseAuditLog>> GetRecentLogsByDeviceHashAsync(string deviceHash, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.DeviceHash == deviceHash && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AbuseAuditLog>> GetRecentLogsByUserIdAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
