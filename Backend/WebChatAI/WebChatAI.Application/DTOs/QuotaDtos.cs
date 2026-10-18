namespace WebChatAI.Application.DTOs;

public record CreditConsumptionResult(
    bool Allowed,
    bool IsByokBypass,
    int RemainingCredits,
    string? RejectionReason = null,
    DateTime? ResetAt = null
);

public record DeviceRegistrationRiskResult(
    bool IsAllowed,
    bool ShouldAllocateFreeCredits,
    int InitialCreditLimit,
    string DeviceHash,
    int RiskScore,
    string? WarningMessage = null
);
