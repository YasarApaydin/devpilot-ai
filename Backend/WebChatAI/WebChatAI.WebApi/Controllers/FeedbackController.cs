using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Route("api/feedback")]
public class FeedbackController : ApiController
{
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IMediator mediator,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        ILogger<FeedbackController> logger) : base(mediator)
    {
        _emailService = emailService;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SendFeedback([FromForm] FeedbackFormModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Message))
        {
            return BadRequest(ApiResponse<bool>.Fail("Mesaj alanı boş bırakılamaz."));
        }

        string senderEmail = model.Email?.Trim() ?? string.Empty;
        string senderName = model.Name?.Trim() ?? string.Empty;

        // If authenticated, strictly enforce real user details from database (tamper-proof)
        if (_currentUserService.UserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
            if (user != null)
            {
                senderEmail = user.Email;
                senderName = $"{user.FirstName} {user.LastName}".Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            senderEmail = "anonim@devpilot.local";
        }
        if (string.IsNullOrWhiteSpace(senderName))
        {
            senderName = "DevPilot Kullanıcısı";
        }

        string? attachmentFileName = null;
        byte[]? attachmentBytes = null;
        string? attachmentContentType = null;

        if (model.Attachment != null && model.Attachment.Length > 0)
        {
            // Max 15 MB limit
            if (model.Attachment.Length > 15 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<bool>.Fail("Ek dosya boyutu 15 MB'tan büyük olamaz."));
            }

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".pdf", ".txt", ".log", ".json", ".md" };
            var ext = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(ApiResponse<bool>.Fail("Yalnızca resim (PNG, JPG, WEBP), PDF veya metin/log (.txt, .log) dosyaları yüklenebilir."));
            }

            attachmentFileName = Path.GetFileName(model.Attachment.FileName);
            attachmentContentType = model.Attachment.ContentType;

            using var memoryStream = new MemoryStream();
            await model.Attachment.CopyToAsync(memoryStream, cancellationToken);
            attachmentBytes = memoryStream.ToArray();
        }

        try
        {
            await _emailService.SendFeedbackEmailAsync(
                senderEmail: senderEmail,
                senderName: senderName,
                category: model.Category,
                message: model.Message,
                attachmentFileName: attachmentFileName,
                attachmentBytes: attachmentBytes,
                attachmentContentType: attachmentContentType,
                cancellationToken: cancellationToken);

            return Ok(ApiResponse<bool>.Ok(true, "Geri bildiriminiz Yaşar Apaydın'a başarıyla ulaştırıldı. İlginiz için teşekkür ederiz!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver feedback email.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<bool>.Fail("Geri bildirim e-postası iletilirken bir sorun oluştu. Lütfen daha sonra tekrar deneyiniz."));
        }
    }
}

public class FeedbackFormModel
{
    public string Category { get; set; } = "Hata / Bug Bildirimi";
    public string Message { get; set; } = default!;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public IFormFile? Attachment { get; set; }
}
