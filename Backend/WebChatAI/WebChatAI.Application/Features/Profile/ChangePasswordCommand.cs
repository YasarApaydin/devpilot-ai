using FluentValidation;
using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Profile;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<ApiResponse>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("New password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IEncryptionService encryptionService,
        IRefreshTokenRepository refreshTokenRepository,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _refreshTokenRepository = refreshTokenRepository;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse.FailureResult("Unauthorized");
        }

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse.FailureResult("User not found.");
        }

        if (!_encryptionService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return ApiResponse.FailureResult("Current password is incorrect.");
        }

        user.PasswordHash = _encryptionService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _userRepository.Update(user);

        // Invalidate active refresh tokens
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
            // Do not fail password change if email service encounters temporary issues
        }

        return ApiResponse.SuccessResult("Password changed successfully.");
    }
}
