using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class SecurityScanFinding : BaseEntity
{
    public Guid SecurityScanId { get; set; }
    public SecurityScan SecurityScan { get; set; } = default!;

    public string Title { get; set; } = default!;
    // "critical", "high", "medium", "low", "info"
    public string Severity { get; set; } = "info";
    public string? Cwe { get; set; }
    public double? CvssScore { get; set; }
    public string? CvssVector { get; set; }
    public string TargetUrl { get; set; } = default!;
    public string? Endpoint { get; set; }
    public string Description { get; set; } = default!;
    public string? Impact { get; set; }
    public string? TechnicalAnalysis { get; set; }
    public string? Remediation { get; set; }
    public string? Evidence { get; set; }
    public string? PoCDescription { get; set; }
    public string? Confidence { get; set; }
    public string? StrixVulnId { get; set; }
}
