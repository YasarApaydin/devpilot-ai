using WebChatAI.Application.DTOs;

namespace WebChatAI.Application.Interfaces;

public interface IQuotaService
{
    Task<CreditConsumptionResult> TryConsumeCreditAsync(
        Guid userId,
        string? provider,
        string? deviceId,
        string? ipAddress,
        CancellationToken cancellationToken = default
    );

    Task<UserCreditDto> GetQuotaStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task InitializeUserQuotaAsync(
        Guid userId,
        int initialCreditLimit,
        string? deviceHash,
        CancellationToken cancellationToken = default
    );

    Task ResetDailyQuotaIfExpiredAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> RefundCreditAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
