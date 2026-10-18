using FluentValidation;
using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public record MessageResponse(string Message);

public sealed record ResendVerificationCommand(string Email) : IRequest<ApiResponse<MessageResponse>>;

public sealed class ResendVerificationCommandValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Invalid email format.");
    }
}

public sealed class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, ApiResponse<MessageResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEncryptionService encryptionService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _encryptionService = encryptionService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<MessageResponse>> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
        {
            // Do not disclose user non-existence for security
            return ApiResponse<MessageResponse>.Ok(
                new MessageResponse("Verification code resent successfully."),
                "Verification code resent."
            );
        }

        if (user.IsEmailVerified)
        {
            return ApiResponse<MessageResponse>.Ok(
                new MessageResponse("This email address is already verified."),
                "Already verified."
            );
        }

        // Önceki tüm aktif kodları kesinlikle iptal et / geçersiz kıl!
        await _tokenRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

        var code = Random.Shared.Next(100000, 999999).ToString();
        var token = Guid.NewGuid().ToString("N");
        var tokenHash = _encryptionService.HashPassword(token);
        var codeHash = _encryptionService.HashPassword(code);

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendVerificationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(), code, cancellationToken);

        return ApiResponse<MessageResponse>.Ok(
            new MessageResponse("Verification code resent successfully."),
            "Verification code resent."
        );
    }
}
