using System.Text;
using WebChatAI.Domain.Entities;
using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class SecurityScanPdfServiceTests
{
    private readonly SecurityScanPdfService _service = new();

    [Fact]
    public void GenerateScanReportPdf_ValidScanAndFindings_ReturnsValidPdfBytes()
    {
        var scan = new SecurityScan
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TargetUrl = "https://test.example.com",
            NormalizedTargetUrl = "https://test.example.com/",
            ScanMode = "quick",
            SelectedCategories = "security_headers,info_disclosure",
            Status = "Completed",
            TotalFindingsCount = 1,
            CriticalCount = 1
        };

        var findings = new List<SecurityScanFinding>
        {
            new SecurityScanFinding
            {
                Id = Guid.NewGuid(),
                SecurityScanId = scan.Id,
                Title = "Eksik HSTS Guvenlik Basligi",
                Severity = "high",
                Cwe = "CWE-319",
                CvssScore = 7.5,
                TargetUrl = scan.NormalizedTargetUrl,
                Endpoint = "/",
                Description = "Hedef uygulamada HSTS başlığı bulunamadı.",
                Impact = "Trafik düz metin HTTP üzerinden dinlenebilir.",
                Remediation = "Strict-Transport-Security başlığı eklenmelidir."
            }
        };

        var consent = new SecurityScanConsent
        {
            Id = Guid.NewGuid(),
            SecurityScanId = scan.Id,
            UserId = scan.UserId,
            TargetUrl = scan.NormalizedTargetUrl,
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true,
            ConsentedAt = DateTime.UtcNow,
            DisclaimerVersion = "v1.0"
        };

        var pdfBytes = _service.GenerateScanReportPdf(scan, findings, consent);

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);

        var pdfHeader = Encoding.ASCII.GetString(pdfBytes.Take(8).ToArray());
        Assert.StartsWith("%PDF-1.", pdfHeader);

        var pdfFull = Encoding.ASCII.GetString(pdfBytes);
        Assert.Contains("%%EOF", pdfFull);
        Assert.Contains("HSTS", pdfFull);
    }

    [Fact]
    public void GenerateConsentReportPdf_ValidConsent_ReturnsValidPdfBytes()
    {
        var scan = new SecurityScan
        {
            Id = Guid.NewGuid(),
            TargetUrl = "https://test.example.com",
            ScanMode = "deep"
        };

        var consent = new SecurityScanConsent
        {
            Id = Guid.NewGuid(),
            SecurityScanId = scan.Id,
            UserId = Guid.NewGuid(),
            TargetUrl = scan.TargetUrl,
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true,
            ConsentedAt = DateTime.UtcNow,
            ClientIp = "192.0.2.1"
        };

        var pdfBytes = _service.GenerateConsentReportPdf(scan, consent);

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);

        var pdfHeader = Encoding.ASCII.GetString(pdfBytes.Take(8).ToArray());
        Assert.StartsWith("%PDF-1.", pdfHeader);
    }
}
