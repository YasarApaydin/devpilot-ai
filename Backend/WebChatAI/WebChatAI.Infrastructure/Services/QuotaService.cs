using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Infrastructure.Services;

public class QuotaService : IQuotaService
{
    private readonly IUserQuotaRepository _quotaRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserAiKeyRepository _userAiKeyRepository;
    private readonly IAbuseDetectionService _abuseDetectionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuotaService> _logger;

    public QuotaService(
        IUserQuotaRepository quotaRepository,
        IUserRepository userRepository,
        IUserAiKeyRepository userAiKeyRepository,
        IAbuseDetectionService abuseDetectionService,
        IUnitOfWork unitOfWork,
        ILogger<QuotaService> logger)
    {
        _quotaRepository = quotaRepository;
        _userRepository = userRepository;
        _userAiKeyRepository = userAiKeyRepository;
        _abuseDetectionService = abuseDetectionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreditConsumptionResult> TryConsumeCreditAsync(
        Guid userId,
        string? provider,
        string? deviceId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // 1. BYOK Exemption Check: User using their own API key does NOT consume DevPilot credits
        if (!string.IsNullOrWhiteSpace(provider) && !provider.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            var userKey = await _userAiKeyRepository.GetByUserAndProviderAsync(userId, provider, cancellationToken);
            if (userKey != null && userKey.IsActive && userKey.IsValid)
            {
                var currentQuota = await _quotaRepository.GetByUserIdAsync(userId, cancellationToken);
                return new CreditConsumptionResult(
                    Allowed: true,
                    IsByokBypass: true,
                    RemainingCredits: currentQuota?.RemainingCredits ?? 15
                );
            }
        }

        // 2. Ensure quota row exists
        var quota = await _quotaRepository.GetByUserIdAsync(userId, cancellationToken);
        if (quota == null)
        {
            quota = new UserQuota
            {
                UserId = userId,
                DailyCreditLimit = 15,
                CreditsUsedToday = 0,
                CycleStartDate = DateTime.UtcNow.Date,
                SubscriptionTier = "free",
                IsExemptFromAbuseLimits = false
            };
            await _quotaRepository.AddAsync(quota, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Quota record may have been created concurrently for user {UserId}", userId);
            }
        }

        // Exemption check for admin/premium tiers
        if (quota.IsExemptFromAbuseLimits || quota.SubscriptionTier.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
        {
            return new CreditConsumptionResult(
                Allowed: true,
                IsByokBypass: false,
                RemainingCredits: 9999
            );
        }

        // 3. Multi-instance atomic conditional consumption at the database level
        var consumed = await _quotaRepository.TryAtomicConsumeCreditAsync(userId, cancellationToken);
        if (!consumed)
        {
            var nextReset = quota.CycleStartDate.Date.AddDays(1);

            await _abuseDetectionService.LogAbuseEventAsync(
                userId: userId,
                deviceHash: quota.AllocatedByDeviceHash,
                ipAddress: ipAddress ?? "unknown",
                eventType: "QUOTA_EXHAUSTED",
                severity: "INFO",
                riskScore: 0,
                details: $"User attempted request with 0 remaining credits. Limit: {quota.DailyCreditLimit}, Used: {quota.CreditsUsedToday}",
                cancellationToken: cancellationToken
            );

            return new CreditConsumptionResult(
                Allowed: false,
                IsByokBypass: false,
                RemainingCredits: 0,
                RejectionReason: "Günlük 15 mesaj kredi limitinize ulaştınız. Kredileriniz 24 saat sonra yenilenecektir. Kendi API anahtarınızı (BYOK) ekleyerek sınırsız sohbete devam edebilirsiniz.",
                ResetAt: nextReset
            );
        }

        var refreshedQuota = await _quotaRepository.GetByUserIdNoTrackingAsync(userId, cancellationToken);
        var remainingCredits = refreshedQuota?.RemainingCredits ?? Math.Max(0, quota.DailyCreditLimit - (quota.CreditsUsedToday + 1));

        _logger.LogInformation("Credit consumed for user {UserId}. Remaining: {Remaining}", userId, remainingCredits);

        return new CreditConsumptionResult(
            Allowed: true,
            IsByokBypass: false,
            RemainingCredits: remainingCredits
        );
    }

    public async Task<UserCreditDto> GetQuotaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var email = user?.Email ?? string.Empty;

        // 1. Atomically reset cycle if today is a new cycle without overwriting any concurrent consumption
        await _quotaRepository.EnsureCycleResetAsync(userId, cancellationToken);

        // 2. Read latest quota without tracking to prevent EF change tracker caching or stale entity saves
        var quota = await _quotaRepository.GetByUserIdNoTrackingAsync(userId, cancellationToken);
        if (quota == null)
        {
            quota = new UserQuota
            {
                UserId = userId,
                DailyCreditLimit = 15,
                CreditsUsedToday = 0,
                CycleStartDate = DateTime.UtcNow.Date,
                SubscriptionTier = "free"
            };
            await _quotaRepository.AddAsync(quota, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var nextResetAt = quota.CycleStartDate.Date.AddDays(1);

        var keys = await _userAiKeyRepository.GetUserKeysAsync(userId, cancellationToken);
        var hasActiveByok = keys.Any(k => k.IsActive && k.IsValid);

        return new UserCreditDto(
            Email: email,
            TotalCredits: quota.DailyCreditLimit,
            UsedCredits: quota.CreditsUsedToday,
            RemainingCredits: quota.RemainingCredits,
            NextResetAt: nextResetAt,
            SubscriptionTier: quota.SubscriptionTier,
            IsByokActive: hasActiveByok
        );
    }

    public async Task InitializeUserQuotaAsync(
        Guid userId,
        int initialCreditLimit,
        string? deviceHash,
        CancellationToken cancellationToken = default)
    {
        var existing = await _quotaRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existing != null)
        {
            existing.DailyCreditLimit = initialCreditLimit;
            existing.AllocatedByDeviceHash = deviceHash;
            _quotaRepository.Update(existing);
        }
        else
        {
            var quota = new UserQuota
            {
                UserId = userId,
                DailyCreditLimit = initialCreditLimit,
                CreditsUsedToday = 0,
                CycleStartDate = DateTime.UtcNow.Date,
                SubscriptionTier = "free",
                AllocatedByDeviceHash = deviceHash
            };
            await _quotaRepository.AddAsync(quota, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetDailyQuotaIfExpiredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var quota = await _quotaRepository.GetByUserIdAsync(userId, cancellationToken);
        if (quota != null)
        {
            var prevDate = quota.CycleStartDate;
            quota.CheckAndResetCycle();
            if (quota.CycleStartDate != prevDate)
            {
                _quotaRepository.Update(quota);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<bool> RefundCreditAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _quotaRepository.TryRefundCreditAsync(userId, cancellationToken);
    }
}
