using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public sealed record RefreshTokenCommand(string? RawRefreshToken = null) : IRequest<ApiResponse<AuthResponseDto>>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return ApiResponse<AuthResponseDto>.Fail("No refresh token provided.");
        }

        var tokenHash = _tokenService.HashToken(request.RawRefreshToken);
        var existingRefreshToken = await _refreshTokenRepository.Where(r => r.TokenHash == tokenHash && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingRefreshToken == null || existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");
        }

        if (!existingRefreshToken.IsActive)
        {
            // Handle concurrent refresh requests arriving within a 5-second grace window of token rotation
            var recentlyRotated = existingRefreshToken.RevokedAt.HasValue
                && (DateTime.UtcNow - existingRefreshToken.RevokedAt.Value) <= TimeSpan.FromSeconds(5);

            if (recentlyRotated)
            {
                var hasActiveRotatedToken = await _refreshTokenRepository
                    .Where(r => r.UserId == existingRefreshToken.UserId && r.RevokedAt == null && !r.IsDeleted && r.ExpiresAt > DateTime.UtcNow)
                    .AnyAsync(cancellationToken);

                if (hasActiveRotatedToken)
                {
                    var activeUser = await _userRepository.GetByIdAsync(existingRefreshToken.UserId, cancellationToken);
                    if (activeUser != null && activeUser.IsActive)
                    {
                        var graceAccessToken = _tokenService.GenerateAccessToken(activeUser);
                        var graceUserDto = new UserDto(
                            Id: activeUser.Id,
                            Email: activeUser.Email,
                            FirstName: activeUser.FirstName,
                            LastName: activeUser.LastName,
                            EmailVerified: activeUser.IsEmailVerified,
                            AvatarUrl: null,
                            CreatedAt: activeUser.CreatedAt
                        );
                        return ApiResponse<AuthResponseDto>.Ok(
                            new AuthResponseDto(graceUserDto, graceAccessToken, 3600, null),
                            "Token refreshed successfully."
                        );
                    }
                }
            }

            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(existingRefreshToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<AuthResponseDto>.Fail("User account is inactive or not found.");
        }

        // Rotate token: revoke old one
        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepository.Update(existingRefreshToken);

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashToken(newRawRefreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedIp = _currentUserService.IpAddress,
            UserAgent = _currentUserService.UserAgent
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
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
            new AuthResponseDto(userDto, newAccessToken, 3600, newRawRefreshToken),
            "Token refreshed successfully."
        );
    }
}
