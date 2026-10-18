using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
[Route("api/security-scans")]
public class SecurityScanController : ApiController
{
    private readonly ISecurityScanService _scanService;
    private readonly ICurrentUserService _currentUserService;

    public SecurityScanController(
        IMediator mediator,
        ISecurityScanService scanService,
        ICurrentUserService currentUserService) : base(mediator)
    {
        _scanService = scanService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Desteklenen gerçek güvenlik test kategorilerini ve derinlik seçeneklerini listeler.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = _scanService.GetSupportedCategories();
        return Ok(categories);
    }

    /// <summary>
    /// Hedef URL adresini SSRF, DNS ve protokol güvenliği açısından doğrular.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate-target")]
    public async Task<IActionResult> ValidateTarget([FromBody] ValidateTargetRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.TargetUrl))
        {
            return BadRequest(new ValidateTargetResponseDto
            {
                IsValid = false,
                ErrorMessage = "Hedef URL adresi boş olamaz."
            });
        }

        var result = await _scanService.ValidateTargetAsync(request.TargetUrl, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının açık yasal onayı ile yeni bir güvenlik taraması başlatır.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("HeavyWorkloadPolicy")]
    public async Task<IActionResult> CreateScan([FromBody] CreateSecurityScanRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        if (!request.HasOwnershipAuthorization || !request.HasRiskAcknowledged)
        {
            return BadRequest(new
            {
                error = "ConsentRequired",
                message = "Güvenlik taraması başlatılabilmesi için hedef sistem üzerindeki yasal yetki ve risk beyanlarının eksiksiz onaylanması zorunludur."
            });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var scan = await _scanService.CreateAndStartScanAsync(
                _currentUserService.UserId.Value,
                request,
                clientIp,
                userAgent,
                cancellationToken);

            return CreatedAtAction(nameof(GetScanById), new { id = scan.Id }, scan);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "ValidationError", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("kredi", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "QUOTA_EXHAUSTED", message = ex.Message });
            }
            return Conflict(new { error = "OperationFailed", message = ex.Message });
        }
    }

    /// <summary>
    /// Giriş yapmış kullanıcıya ait güvenlik taramalarını listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyScans([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var scans = await _scanService.GetUserScansAsync(_currentUserService.UserId.Value, page, pageSize, cancellationToken);
        return Ok(scans);
    }

    /// <summary>
    /// Belirtilen taramanın durumunu ve bulgularını getirir (yalnızca tarama sahibi erişebilir).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetScanById(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var scan = await _scanService.GetScanByIdAsync(id, _currentUserService.UserId.Value, cancellationToken);
        if (scan == null)
        {
            return NotFound(new { error = "NotFound", message = "Güvenlik taraması bulunamadı veya bu taramaya erişim yetkiniz yok." });
        }

        return Ok(scan);
    }

    /// <summary>
    /// Devam eden bir güvenlik taramasını iptal eder.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelScan(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var cancelled = await _scanService.CancelScanAsync(id, _currentUserService.UserId.Value, cancellationToken);
        if (!cancelled)
        {
            return BadRequest(new { message = "Tarama iptal edilemedi. Tarama tamamlanmış, zaten iptal edilmiş veya bulunamamış olabilir." });
        }

        return Ok(new { message = "Tarama başarıyla iptal edildi." });
    }

    /// <summary>
    /// Güvenlik taraması teknik değerlendirme raporunu PDF olarak indirir.
    /// </summary>
    [HttpGet("{id:guid}/report/pdf")]
    public async Task<IActionResult> DownloadReportPdf(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var pdfBytes = await _scanService.GenerateScanPdfReportAsync(id, _currentUserService.UserId.Value, cancellationToken);
            return File(pdfBytes, "application/pdf", $"DevPilot-Security-Report-{id:N}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tarama bulunamadı veya erişim yetkiniz yok." });
        }
    }

    /// <summary>
    /// Yasal yetki ve onay kaydı belgesini PDF olarak indirir.
    /// </summary>
    [HttpGet("{id:guid}/consent/pdf")]
    public async Task<IActionResult> DownloadConsentPdf(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var pdfBytes = await _scanService.GenerateConsentPdfReportAsync(id, _currentUserService.UserId.Value, cancellationToken);
            return File(pdfBytes, "application/pdf", $"DevPilot-Scan-Consent-{id:N}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Onay belgesi bulunamadı veya erişim yetkiniz yok." });
        }
    }
}
