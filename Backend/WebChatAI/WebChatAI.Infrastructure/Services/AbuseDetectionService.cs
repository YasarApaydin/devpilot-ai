using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Infrastructure.Services;

public class AbuseDetectionService : IAbuseDetectionService
{
    private readonly IDeviceRegistrationRepository _deviceRepository;
    private readonly IAbuseAuditLogRepository _abuseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AbuseDetectionService> _logger;
    private readonly string _salt;

    public AbuseDetectionService(
        IDeviceRegistrationRepository deviceRepository,
        IAbuseAuditLogRepository abuseAuditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<AbuseDetectionService> logger,
        IConfiguration configuration)
    {
        _deviceRepository = deviceRepository;
        _abuseAuditLogRepository = abuseAuditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _salt = configuration["Security:DeviceSalt"] ?? "DevPilot_AntiAbuse_Salt_2026_Secured_Key";
    }

    public string HashDeviceId(string? rawDeviceId, string? userAgent, string? ipAddress)
    {
        string inputToHash;
        if (!string.IsNullOrWhiteSpace(rawDeviceId) && rawDeviceId.Trim().Length >= 8)
        {
            inputToHash = $"device:{rawDeviceId.Trim().ToLowerInvariant()}:{_salt}";
        }
        else
        {
            var cleanUa = (userAgent ?? "unknown").Trim().ToLowerInvariant();
            var subnet = GetSubnet(ipAddress);
            inputToHash = $"fallback:{cleanUa}:{subnet}:{_salt}";
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputToHash));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GetSubnet(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return "unknown";
        var parts = ipAddress.Trim().Split('.');
        if (parts.Length == 4)
        {
            // /24 subnet for IPv4
            return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        }
        // IPv6 or unknown
        return ipAddress.Length > 16 ? ipAddress[..16] : ipAddress;
    }

    public async Task<DeviceRegistrationRiskResult> EvaluateRegistrationRiskAsync(
        string? rawDeviceId,
        string? ipAddress,
        string? userAgent,
        string email,
        CancellationToken cancellationToken = default)
    {
        var deviceHash = HashDeviceId(rawDeviceId, userAgent, ipAddress);
        var existingDevice = await _deviceRepository.GetByDeviceHashAsync(deviceHash, cancellationToken);

        if (existingDevice == null)
        {
            var newDevice = new DeviceRegistration
            {
                DeviceHash = deviceHash,
                LastIpAddress = ipAddress,
                UserAgent = userAgent,
                RegistrationCount = 1,
                DailyRegistrationsCount = 1,
                FirstSeenAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                LastRegisteredDate = DateTime.UtcNow.Date,
                RiskScore = 0,
                IsFlagged = false
            };

            await _deviceRepository.AddAsync(newDevice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New device registered successfully. Hash: {DeviceHash}", deviceHash);

            return new DeviceRegistrationRiskResult(
                IsAllowed: true,
                ShouldAllocateFreeCredits: true,
                InitialCreditLimit: 15,
                DeviceHash: deviceHash,
                RiskScore: 0
            );
        }

        var today = DateTime.UtcNow.Date;
        var isSameDayRegistration = existingDevice.LastRegisteredDate.Date == today;

        existingDevice.RecordNewRegistration(ipAddress, userAgent);
        _deviceRepository.Update(existingDevice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isSameDayRegistration && existingDevice.DailyRegistrationsCount > 1)
        {
            var warning = "Bu cihazdan bugün daha önce bir hesap oluşturuldu. Ücretsiz günlük 15 kredi kotası cihaz başına 24 saatte bir kez verilir. Kendi API anahtarınızı (BYOK) tanımlayarak sınırsız kullanabilir veya mevcut hesabınıza giriş yapabilirsiniz.";

            await LogAbuseEventAsync(
                userId: null,
                deviceHash: deviceHash,
                ipAddress: ipAddress ?? "unknown",
                eventType: "DEVICE_DAILY_LIMIT_EXCEEDED",
                severity: "WARNING",
                riskScore: 25,
                details: $"Device registered multiple accounts today. Count: {existingDevice.DailyRegistrationsCount}. Email: {email}",
                cancellationToken: cancellationToken
            );

            _logger.LogWarning("Multi-registration detected on device {DeviceHash}. DailyCount: {DailyCount}. Allocation set to 0.",
                deviceHash, existingDevice.DailyRegistrationsCount);

            return new DeviceRegistrationRiskResult(
                IsAllowed: true,
                ShouldAllocateFreeCredits: false,
                InitialCreditLimit: 0,
                DeviceHash: deviceHash,
                RiskScore: existingDevice.RiskScore,
                WarningMessage: warning
            );
        }

        return new DeviceRegistrationRiskResult(
            IsAllowed: true,
            ShouldAllocateFreeCredits: true,
            InitialCreditLimit: 15,
            DeviceHash: deviceHash,
            RiskScore: existingDevice.RiskScore
        );
    }

    public async Task LogAbuseEventAsync(
        Guid? userId,
        string? deviceHash,
        string ipAddress,
        string eventType,
        string severity,
        int riskScore,
        string details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new AbuseAuditLog
            {
                UserId = userId,
                DeviceHash = deviceHash,
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress,
                EventType = eventType,
                Severity = severity,
                RiskScore = riskScore,
                Details = details
            };

            await _abuseAuditLogRepository.AddAsync(log, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist abuse audit log for EventType: {EventType}", eventType);
        }
    }
}
