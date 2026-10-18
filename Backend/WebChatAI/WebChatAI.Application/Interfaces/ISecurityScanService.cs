using WebChatAI.Application.DTOs;

namespace WebChatAI.Application.Interfaces;

public interface ISecurityScanService
{
    List<ScanCategoryDto> GetSupportedCategories();
    Task<ValidateTargetResponseDto> ValidateTargetAsync(string targetUrl, CancellationToken cancellationToken = default);
    Task<SecurityScanResponseDto> CreateAndStartScanAsync(
        Guid userId,
        CreateSecurityScanRequestDto request,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<SecurityScanResponseDto?> GetScanByIdAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<SecurityScanSummaryDto>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<bool> CancelScanAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateScanPdfReportAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateConsentPdfReportAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default);
}
