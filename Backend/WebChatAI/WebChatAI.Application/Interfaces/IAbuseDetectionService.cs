using WebChatAI.Application.DTOs;

namespace WebChatAI.Application.Interfaces;

public interface IAbuseDetectionService
{
    string HashDeviceId(string? rawDeviceId, string? userAgent, string? ipAddress);
    Task<DeviceRegistrationRiskResult> EvaluateRegistrationRiskAsync(string? rawDeviceId, string? ipAddress, string? userAgent, string email, CancellationToken cancellationToken = default);
    Task LogAbuseEventAsync(Guid? userId, string? deviceHash, string ipAddress, string eventType, string severity, int riskScore, string details, CancellationToken cancellationToken = default);
}
