using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface ISecurityScanConsentRepository : IRepository<SecurityScanConsent>
{
    Task<SecurityScanConsent?> GetByScanIdAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
}
