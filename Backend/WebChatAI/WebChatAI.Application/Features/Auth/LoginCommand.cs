using FluentValidation;
using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public sealed record LoginCommand(
    string Email,
    string Password,
    bool? RememberMe = false
) : IRequest<ApiResponse<AuthResponseDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Invalid email format.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEncryptionService encryptionService,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        ILoginAttemptService loginAttemptService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _encryptionService = encryptionService;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _loginAttemptService = loginAttemptService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ip = _currentUserService.IpAddress ?? "unknown";

        // Check if IP is currently locked out
        if (_loginAttemptService.IsBlocked(ip, out var remainingTime))
        {
            var minutes = Math.Ceiling(remainingTime?.TotalMinutes ?? 15);
            return ApiResponse<AuthResponseDto>.Fail(
                $"Çok fazla başarısız giriş denemesi yapıldı. Güvenliğiniz için IP adresiniz {minutes} dakika kilitlenmiştir. Lütfen daha sonra tekrar deneyiniz.");
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
        {
            _loginAttemptService.RecordFailedAttempt(ip, request.Email);
            var remaining = Math.Max(0, 5 - _loginAttemptService.GetFailedAttempts(ip));
            var warning = remaining > 0 ? $" (Kalan deneme hakkı: {remaining})" : " (IP adresi 15 dakika kilitlendi)";
            return ApiResponse<AuthResponseDto>.Fail($"E-posta adresiniz veya şifreniz hatalıdır.{warning}");
        }

        var isPasswordValid = _encryptionService.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _loginAttemptService.RecordFailedAttempt(ip, request.Email);
            var remaining = Math.Max(0, 5 - _loginAttemptService.GetFailedAttempts(ip));
            var warning = remaining > 0 ? $" (Kalan deneme hakkı: {remaining})" : " (IP adresi 15 dakika kilitlendi)";
            return ApiResponse<AuthResponseDto>.Fail($"E-posta adresiniz veya şifreniz hatalıdır.{warning}");
        }

        if (!user.IsEmailVerified)
        {
            return ApiResponse<AuthResponseDto>.Fail("E-posta adresiniz henüz doğrulanmamıştır. Lütfen e-postanıza gönderilen doğrulama kodunu onaylayın.");
        }

        // Login successful: reset failed attempt counter
        _loginAttemptService.ResetAttempts(ip, request.Email);

        user.LastLoginAt = DateTime.UtcNow;
        _userRepository.Update(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(rawRefreshToken);

        // Revoke older refresh tokens
        await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, cancellationToken);

        var daysValid = (request.RememberMe == true) ? 30 : 1;
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(daysValid),
            CreatedIp = _currentUserService.IpAddress,
            UserAgent = _currentUserService.UserAgent
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            EmailVerified: user.IsEmailVerified,
            AvatarUrl: null,
            CreatedAt: user.CreatedAt
        );

        return ApiResponse<AuthResponseDto>.Ok(
            new AuthResponseDto(userDto, accessToken, 3600, rawRefreshToken),
            "Login successful."
        );
    }
}
