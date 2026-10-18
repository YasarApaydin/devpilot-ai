using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class SecurityScanConsent : BaseEntity
{
    public Guid SecurityScanId { get; set; }
    public SecurityScan SecurityScan { get; set; } = default!;

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string TargetUrl { get; set; } = default!;
    public string DisclaimerVersion { get; set; } = "v1.0";

    public bool HasOwnershipAuthorization { get; set; }
    public bool HasRiskAcknowledged { get; set; }

    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
}
