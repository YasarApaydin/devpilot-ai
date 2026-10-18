using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class AbuseAuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? DeviceHash { get; set; }
    public string IpAddress { get; set; } = "unknown";
    public string EventType { get; set; } = default!; // DISPOSABLE_EMAIL_BLOCKED, DEVICE_LIMIT_EXCEEDED, QUOTA_EXHAUSTED, CONCURRENT_RACE_DETECTED, etc.
    public string Severity { get; set; } = "INFO"; // INFO, WARNING, HIGH, CRITICAL
    public int RiskScore { get; set; } = 0;
    public string Details { get; set; } = string.Empty;
}
