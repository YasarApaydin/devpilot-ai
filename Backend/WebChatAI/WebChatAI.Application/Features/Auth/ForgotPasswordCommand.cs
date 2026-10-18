using FluentValidation;
using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public sealed record ForgotPasswordCommand(string Email, string? FrontendBaseUrl = null) : IRequest<ApiResponse<MessageResponse>>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Invalid email format.");
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<MessageResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(
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

    public async Task<ApiResponse<MessageResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user != null && user.IsActive)
        {
            var token = Guid.NewGuid().ToString("N");
            var tokenHash = _encryptionService.HashPassword(token);

            var resetToken = new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            await _tokenRepository.AddAsync(resetToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendPasswordResetEmailAsync(user.Email, token, frontendBaseUrl: null, cancellationToken);
        }

        // Generic safe message to prevent email enumeration
        return ApiResponse<MessageResponse>.Ok(
            new MessageResponse("If an account exists for this email, a password reset email has been sent."),
            "Password reset request received."
        );
    }
}
