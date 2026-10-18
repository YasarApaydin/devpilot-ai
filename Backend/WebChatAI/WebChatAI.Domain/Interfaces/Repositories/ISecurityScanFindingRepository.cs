using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface ISecurityScanFindingRepository : IRepository<SecurityScanFinding>
{
    Task<List<SecurityScanFinding>> GetByScanIdAsync(Guid scanId, CancellationToken cancellationToken = default);
}
