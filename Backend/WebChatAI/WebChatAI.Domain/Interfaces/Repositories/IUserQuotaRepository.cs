using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IUserQuotaRepository : IRepository<UserQuota>
{
    Task<UserQuota?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserQuota?> GetByUserIdNoTrackingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> EnsureCycleResetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> TryAtomicConsumeCreditAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> TryRefundCreditAsync(Guid userId, CancellationToken cancellationToken = default);
}
