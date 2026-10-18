using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string? ConfirmPassword = null
) : IRequest<ApiResponse<MessageResponse>>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Reset token is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .When(x => !string.IsNullOrEmpty(x.ConfirmPassword))
            .WithMessage("Passwords do not match.");
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<MessageResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEncryptionService encryptionService,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _encryptionService = encryptionService;
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<MessageResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Limit active tokens to top 20 recent with AttemptCount < 5 (K3 DoS & brute-force protection)
        var activeTokens = await _tokenRepository.Where(t => t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted && t.AttemptCount < 5)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var matchedToken = activeTokens.FirstOrDefault(t => _encryptionService.VerifyPassword(request.Token, t.TokenHash));
        if (matchedToken == null)
        {
            // Do not mutate or poison unrelated users' tokens (DEV-SEC-006 fix)
            return ApiResponse<MessageResponse>.Fail("Invalid or expired password reset token.");
        }

        var user = await _userRepository.GetByIdAsync(matchedToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<MessageResponse>.Fail("User account not found or disabled.");
        }

        user.PasswordHash = _encryptionService.HashPassword(request.NewPassword);
        _userRepository.Update(user);

        matchedToken.UsedAt = DateTime.UtcNow;
        _tokenRepository.Update(matchedToken);

        // Revoke all existing sessions for safety
        await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendPasswordChangedNotificationEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Do not fail password reset if email delivery encounters temporary issues
        }

        return ApiResponse<MessageResponse>.Ok(
            new MessageResponse("Password updated successfully."),
            "Password reset successful."
        );
    }
}