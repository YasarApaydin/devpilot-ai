using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public record VerifyEmailResponse(bool Verified, string Message);

public sealed record VerifyEmailCommand(string? Token = null, string? Code = null, string? Email = null) : IRequest<ApiResponse<VerifyEmailResponse>>;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, ApiResponse<VerifyEmailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEncryptionService encryptionService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<VerifyEmailResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) && string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResponse<VerifyEmailResponse>.Fail("Verification token or code is required.");
        }

        Domain.Entities.EmailVerificationToken? matchedToken = null;
        List<Domain.Entities.EmailVerificationToken> candidateTokens;

        Domain.Entities.User? targetUser = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            targetUser = await _userRepository.GetByNormalizedEmailAsync(request.Email.Trim().ToUpperInvariant(), cancellationToken);
        }

        if (targetUser != null)
        {
            candidateTokens = await _tokenRepository.Where(t => t.UserId == targetUser.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted && t.AttemptCount < 5)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Limit candidate pool to recent active tokens
            candidateTokens = await _tokenRepository.Where(t => t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow && !t.IsDeleted && t.AttemptCount < 5)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            matchedToken = candidateTokens.FirstOrDefault(t => _encryptionService.VerifyPassword(request.Token, t.TokenHash));
        }

        if (matchedToken == null && !string.IsNullOrWhiteSpace(request.Code))
        {
            matchedToken = candidateTokens.FirstOrDefault(t => t.CodeHash != null && _encryptionService.VerifyPassword(request.Code, t.CodeHash));
        }

        if (matchedToken == null)
        {
            // Strictly increment attempt count only for the targeted user's token, never across other users
            if (targetUser != null && candidateTokens.Count > 0)
            {
                var targetToken = candidateTokens.First();
                targetToken.AttemptCount++;
                if (targetToken.AttemptCount >= 5)
                {
                    targetToken.IsDeleted = true;
                }
                _tokenRepository.Update(targetToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ApiResponse<VerifyEmailResponse>.Fail("Girdiğiniz doğrulama kodu geçersizdir veya süresi dolmuştur. Yeni bir kod aldıysanız lütfen en son gelen kodu kullanınız.");
        }

        var user = await _userRepository.GetByIdAsync(matchedToken.UserId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<VerifyEmailResponse>.Fail("Kullanıcı bulunamadı.");
        }

        matchedToken.UsedAt = DateTime.UtcNow;
        _tokenRepository.Update(matchedToken);

        // Invalidate all tokens for user once verified
        await _tokenRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

        user.IsEmailVerified = true;
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<VerifyEmailResponse>.Ok(
            new VerifyEmailResponse(true, "E-posta adresiniz başarıyla doğrulandı."),
            "E-posta başarıyla doğrulandı."
        );
    }
}