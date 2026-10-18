using WebChatAI.Domain.Entities;

namespace WebChatAI.Application.Interfaces;

public interface ISecurityScanRunnerService
{
    Task RunScanInBackgroundAsync(Guid scanId, CancellationToken cancellationToken = default);
    bool TryCancelScan(Guid scanId);
}
