using Moq;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Infrastructure.Services;
using Xunit;

namespace WebChatAI.Tests;

public class SecurityScanServiceTests
{
    private readonly Mock<ISecurityScanRepository> _scanRepoMock = new();
    private readonly Mock<ISecurityScanConsentRepository> _consentRepoMock = new();
    private readonly Mock<ISecurityScanFindingRepository> _findingRepoMock = new();
    private readonly Mock<ISsrfProtectionService> _ssrfMock = new();
    private readonly Mock<ISecurityScanRunnerService> _runnerMock = new();
    private readonly Mock<ISecurityScanPdfService> _pdfMock = new();
    private readonly Mock<IMessageRepository> _messageRepoMock = new();
    private readonly Mock<IConversationRepository> _convRepoMock = new();
    private readonly Mock<IQuotaService> _quotaMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    private readonly SecurityScanService _service;

    public SecurityScanServiceTests()
    {
        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 14));

        _service = new SecurityScanService(
            _scanRepoMock.Object,
            _consentRepoMock.Object,
            _findingRepoMock.Object,
            _ssrfMock.Object,
            _runnerMock.Object,
            _pdfMock.Object,
            _messageRepoMock.Object,
            _convRepoMock.Object,
            _quotaMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task CreateAndStartScan_WithoutOwnershipAuthorization_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = false, // Not consented
            HasRiskAcknowledged = true
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("yasal yetki", ex.Message);
    }

    [Fact]
    public async Task CreateAndStartScan_WithoutRiskAcknowledgment_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = false // Not acknowledged
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("risk", ex.Message);
    }

    [Fact]
    public async Task CreateAndStartScan_WithSsrfViolation_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "http://192.168.1.1",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = false,
                ErrorMessage = "SSRF Koruması: Özel IP aralığı reddedildi."
            });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("SSRF Koruması", ex.Message);
    }

    [Fact]
    public async Task CreateAndStartScan_WithActiveRunningScan_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // User already has a running scan!

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("aktif bir güvenlik taramanız bulunmaktadır", ex.Message);
    }

    [Fact]
    public async Task CreateAndStartScan_ValidConsentAndTarget_CreatesScanAndStartsRunner()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            ScanMode = "quick",
            SelectedCategories = new List<string> { "security_headers", "info_disclosure" },
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent");

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("https://example.com", result.NormalizedTargetUrl);
        Assert.True(result.Consent?.HasOwnershipAuthorization);
        Assert.True(result.Consent?.HasRiskAcknowledged);

        _scanRepoMock.Verify(r => r.AddAsync(It.IsAny<SecurityScan>(), It.IsAny<CancellationToken>()), Times.Once);
        _consentRepoMock.Verify(c => c.AddAsync(It.IsAny<SecurityScanConsent>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _runnerMock.Verify(m => m.RunScanInBackgroundAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAndStartScan_WithUnsupportedCategory_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            ScanMode = "quick",
            SelectedCategories = new List<string> { "sql_injection_active", "remote_code_execution" },
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("Desteklenmeyen veya uygulanamayan", ex.Message);
    }

    [Fact]
    public async Task GetScanById_OtherUserScan_ReturnsNullForIsolation()
    {
        var scanId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // Repository returns null because query filters for userId
        _scanRepoMock.Setup(r => r.GetScanWithDetailsAsync(scanId, userB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityScan?)null);

        var result = await _service.GetScanByIdAsync(scanId, userB);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAndStartScan_WhenQuotaExhausted_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            userId,
            "security_scan",
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: false, IsByokBypass: false, RemainingCredits: 0, RejectionReason: "Günlük kredi limitiniz doldu."));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("kredi", ex.Message);
    }

    [Fact]
    public async Task CreateAndStartScan_ActiveScanConcurrencyConflict_RefundsCredit()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            userId,
            "security_scan",
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 10));

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate key value violates unique constraint \"uq_security_scans_active_per_user\""));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("aktif bir güvenlik taramanız bulunmaktadır", ex.Message);
        _quotaMock.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAndStartScan_ActiveScanConcurrencyConflict_WithByok_DoesNotRefundCredit()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            userId,
            "security_scan",
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: true, RemainingCredits: 15));

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate key value violates unique constraint \"uq_security_scans_active_per_user\""));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("aktif bir güvenlik taramanız bulunmaktadır", ex.Message);
        _quotaMock.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAndStartScan_GenericDbErrorAfterQuotaConsumed_RefundsCredit()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            userId,
            "security_scan",
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 10));

        // Generic database failure (not a concurrency conflict)
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("PostgreSQL connection terminated unexpectedly"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        Assert.Contains("connection terminated", ex.Message);
        // Verify credit was compensated
        _quotaMock.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAndStartScan_PersistenceFailure_PreventsDoubleRefund()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSecurityScanRequestDto
        {
            TargetUrl = "https://example.com",
            HasOwnershipAuthorization = true,
            HasRiskAcknowledged = true
        };

        _ssrfMock.Setup(s => s.ValidateTargetUrlAsync(request.TargetUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidateTargetResponseDto
            {
                IsValid = true,
                NormalizedUrl = "https://example.com"
            });

        _scanRepoMock.Setup(r => r.HasActiveRunningScanAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _quotaMock.Setup(q => q.TryConsumeCreditAsync(
            userId,
            "security_scan",
            null,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 10));

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate key value violates unique constraint \"uq_security_scans_active_per_user\""));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAndStartScanAsync(userId, request, "127.0.0.1", "TestAgent"));

        // Exactly one refund, never double
        _quotaMock.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
