using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public string? DefaultAiProvider { get; set; }
    public string? DefaultAiModel { get; set; }

    // Navigation properties
    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<SecurityScan> SecurityScans { get; set; } = new List<SecurityScan>();
    public ICollection<SecurityScanConsent> SecurityScanConsents { get; set; } = new List<SecurityScanConsent>();
    public ICollection<UserAiKey> AiKeys { get; set; } = new List<UserAiKey>();
    public UserQuota? Quota { get; set; }
    public ICollection<AbuseAuditLog> AbuseAuditLogs { get; set; } = new List<AbuseAuditLog>();
}
