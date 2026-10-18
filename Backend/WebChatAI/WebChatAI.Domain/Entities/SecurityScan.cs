using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class SecurityScan : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public string TargetUrl { get; set; } = default!;
    public string NormalizedTargetUrl { get; set; } = default!;
    
    // Scan mode: "quick", "standard", "deep"
    public string ScanMode { get; set; } = "quick";

    // Comma-separated or JSON array of category identifiers
    public string SelectedCategories { get; set; } = "security_headers,ssl_tls,cookie_cors_security,info_disclosure,robots_wellknown,form_client_security,recon_discovery,ai_config_audit";

    public string? CustomInstructions { get; set; }

    // Status: "Pending", "Running", "Completed", "Failed", "Cancelled"
    public string Status { get; set; } = "Pending";

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }

    public int TotalFindingsCount { get; set; } = 0;
    public int CriticalCount { get; set; } = 0;
    public int HighCount { get; set; } = 0;
    public int MediumCount { get; set; } = 0;
    public int LowCount { get; set; } = 0;
    public int InfoCount { get; set; } = 0;
    public string? StrixRunId { get; set; }

    // Navigation properties
    public SecurityScanConsent? Consent { get; set; }
    public ICollection<SecurityScanFinding> Findings { get; set; } = new List<SecurityScanFinding>();
}
