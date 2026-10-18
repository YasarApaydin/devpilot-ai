using FluentValidation;
using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public record RegisterResponse(string Message, bool RequiresVerification, string? Warning = null);

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ConfirmPassword = null,
    string? DeviceId = null
) : IRequest<ApiResponse<RegisterResponse>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Invalid email format.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .When(x => !string.IsNullOrEmpty(x.ConfirmPassword))
            .WithMessage("Passwords do not match.");
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<RegisterResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDisposableEmailValidator _disposableEmailValidator;
    private readonly IAbuseDetectionService _abuseDetectionService;
    private readonly IQuotaService _quotaService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEncryptionService encryptionService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IDisposableEmailValidator disposableEmailValidator,
        IAbuseDetectionService abuseDetectionService,
        IQuotaService quotaService,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _encryptionService = encryptionService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _disposableEmailValidator = disposableEmailValidator;
        _abuseDetectionService = abuseDetectionService;
        _quotaService = quotaService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Layer: Check Disposable / Burner Email
        if (_disposableEmailValidator.IsDisposableEmail(request.Email))
        {
            await _abuseDetectionService.LogAbuseEventAsync(
                userId: null,
                deviceHash: null,
                ipAddress: _currentUserService.IpAddress ?? "unknown",
                eventType: "DISPOSABLE_EMAIL_BLOCKED",
                severity: "HIGH",
                riskScore: 50,
                details: $"Disposable email rejected: {request.Email}",
                cancellationToken: cancellationToken
            );

            return ApiResponse<RegisterResponse>.Fail("Geçici veya tek kullanımlık e-posta adresleri güvenlik nedeniyle kabul edilmemektedir. Lütfen kurumsal veya kalıcı bir e-posta adresi kullanın.");
        }

        // 2. Layer: Device Multi-Registration Risk Evaluation
        var deviceId = !string.IsNullOrWhiteSpace(request.DeviceId) ? request.DeviceId : _currentUserService.DeviceId;
        var riskResult = await _abuseDetectionService.EvaluateRegistrationRiskAsync(
            deviceId,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent,
            request.Email,
            cancellationToken
        );

        if (!riskResult.IsAllowed)
        {
            return ApiResponse<RegisterResponse>.Fail("Güvenlik politikası nedeniyle bu cihazdan kayıt işlemi geçici olarak kısıtlanmıştır.");
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
            {
                return ApiResponse<RegisterResponse>.Fail("Bu e-posta adresi ile kayıtlı bir hesap zaten bulunmaktadır.");
            }

            // Doğrulanmamış kullanıcının bilgilerini güncelle ve yeni doğrulama kodu gönder
            existingUser.FirstName = request.FirstName.Trim();
            existingUser.LastName = request.LastName.Trim();
            existingUser.PasswordHash = _encryptionService.HashPassword(request.Password);
            existingUser.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(existingUser);

            // Önceki tüm aktif ve bekleyen doğrulama kodlarını kesinlikle iptal et / geçersiz kıl!
            await _tokenRepository.InvalidateUserTokensAsync(existingUser.Id, cancellationToken);

            var newCode = Random.Shared.Next(100000, 999999).ToString();
            var newToken = Guid.NewGuid().ToString("N");
            var newTokenHash = _encryptionService.HashPassword(newToken);
            var newCodeHash = _encryptionService.HashPassword(newCode);

            var verificationToken = new EmailVerificationToken
            {
                UserId = existingUser.Id,
                TokenHash = newTokenHash,
                CodeHash = newCodeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                AttemptCount = 0
            };

            await _tokenRepository.AddAsync(verificationToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Initialize or adjust user quota with device limit
            await _quotaService.InitializeUserQuotaAsync(
                existingUser.Id,
                riskResult.InitialCreditLimit,
                riskResult.DeviceHash,
                cancellationToken
            );

            await _emailService.SendVerificationEmailAsync(existingUser.Email, $"{existingUser.FirstName} {existingUser.LastName}".Trim(), newCode, cancellationToken);

            return ApiResponse<RegisterResponse>.Ok(
                new RegisterResponse(
                    "Bilgileriniz güncellendi. Yeni doğrulama kodu e-postanıza gönderildi.",
                    true,
                    riskResult.WarningMessage
                ),
                "Doğrulama kodu gönderildi."
            );
        }

        var passwordHash = _encryptionService.HashPassword(request.Password);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            IsActive = true
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // Generate 6 digit code and secure token
        var code = Random.Shared.Next(100000, 999999).ToString();
        var token = Guid.NewGuid().ToString("N");
        var tokenHash = _encryptionService.HashPassword(token);
        var codeHash = _encryptionService.HashPassword(code);

        var newVerification = new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            AttemptCount = 0
        };

        await _tokenRepository.AddAsync(newVerification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Initialize User Quota with calculated credit limit (15 if first registration today, 0 if duplicate device today)
        await _quotaService.InitializeUserQuotaAsync(
            user.Id,
            riskResult.InitialCreditLimit,
            riskResult.DeviceHash,
            cancellationToken
        );

        // Send email (or log if email provider unconfigured)
        await _emailService.SendVerificationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(), code, cancellationToken);

        // Send welcome email with platform introduction & founder support contact
        try
        {
            await _emailService.SendWelcomeEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                cancellationToken: cancellationToken
            );
        }
        catch
        {
            // Do not abort user registration if welcome email delivery encounters temporary issue
        }

        // Notify admin of new registration (apaydinyasar0@gmail.com)
        try
        {
            await _emailService.SendNewUserRegisteredNotificationAsync(
                user.FirstName,
                user.LastName,
                user.Email,
                cancellationToken
            );
        }
        catch
        {
            // Do not abort user registration if admin notification delivery fails
        }

        return ApiResponse<RegisterResponse>.Ok(
            new RegisterResponse(
                "Hesap oluşturuldu. Lütfen e-postanıza gönderilen kodu onaylayın.",
                true,
                riskResult.WarningMessage
            ),
            "Doğrulama kodu gönderildi."
        );
    }
}
