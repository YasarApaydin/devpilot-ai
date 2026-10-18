using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface ISecurityScanRepository : IRepository<SecurityScan>
{
    Task<SecurityScan?> GetScanWithDetailsAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<SecurityScan>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<bool> HasActiveRunningScanAsync(Guid userId, CancellationToken cancellationToken = default);
}
