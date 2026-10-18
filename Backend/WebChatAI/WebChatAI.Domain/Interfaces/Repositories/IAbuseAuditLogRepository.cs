using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IAbuseAuditLogRepository : IRepository<AbuseAuditLog>
{
    Task<List<AbuseAuditLog>> GetRecentLogsByDeviceHashAsync(string deviceHash, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<AbuseAuditLog>> GetRecentLogsByUserIdAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
}
