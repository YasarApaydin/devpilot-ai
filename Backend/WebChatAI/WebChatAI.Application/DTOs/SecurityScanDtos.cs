namespace WebChatAI.Application.DTOs;

public class ValidateTargetRequestDto
{
    public string TargetUrl { get; set; } = default!;
}

public class ValidateTargetResponseDto
{
    public bool IsValid { get; set; }
    public string? NormalizedUrl { get; set; }
    public string? Host { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ScanCategoryDto
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<string> Cwes { get; set; } = new();
    public bool EnabledByDefault { get; set; } = true;
}

public class CreateSecurityScanRequestDto
{
    public string TargetUrl { get; set; } = default!;
    public Guid? ConversationId { get; set; }
    public string ScanMode { get; set; } = "quick"; // "quick", "standard", "deep"
    public List<string> SelectedCategories { get; set; } = new();
    public string? CustomInstructions { get; set; }

    public string DisclaimerVersion { get; set; } = "v1.0";
    public bool HasOwnershipAuthorization { get; set; }
    public bool HasRiskAcknowledged { get; set; }
}

public class SecurityScanConsentDto
{
    public Guid Id { get; set; }
    public Guid SecurityScanId { get; set; }
    public string TargetUrl { get; set; } = default!;
    public string DisclaimerVersion { get; set; } = default!;
    public bool HasOwnershipAuthorization { get; set; }
    public bool HasRiskAcknowledged { get; set; }
    public DateTime ConsentedAt { get; set; }
    public string? ClientIp { get; set; }
}

public class SecurityScanFindingDto
{
    public Guid Id { get; set; }
    public Guid SecurityScanId { get; set; }
    public string Title { get; set; } = default!;
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
}

public class SecurityScanResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ConversationId { get; set; }
    public string TargetUrl { get; set; } = default!;
    public string NormalizedTargetUrl { get; set; } = default!;
    public string ScanMode { get; set; } = default!;
    public List<string> SelectedCategories { get; set; } = new();
    public string? CustomInstructions { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Summary { get; set; }
    public int TotalFindingsCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int InfoCount { get; set; }
    public string? StrixRunId { get; set; }
    public SecurityScanConsentDto? Consent { get; set; }
    public List<SecurityScanFindingDto> Findings { get; set; } = new();
}

public class SecurityScanSummaryDto
{
    public Guid Id { get; set; }
    public string TargetUrl { get; set; } = default!;
    public string ScanMode { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalFindingsCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int InfoCount { get; set; }
}
