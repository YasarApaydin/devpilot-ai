using WebChatAI.Domain.Entities;

namespace WebChatAI.Application.Interfaces;

public interface ISecurityScanPdfService
{
    byte[] GenerateScanReportPdf(SecurityScan scan, List<SecurityScanFinding> findings, SecurityScanConsent? consent);
    byte[] GenerateConsentReportPdf(SecurityScan scan, SecurityScanConsent consent);
}
