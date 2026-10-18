using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Auth;

public sealed record LogoutCommand : IRequest<ApiResponse>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId.HasValue)
        {
            await _refreshTokenRepository.RevokeUserTokensAsync(_currentUserService.UserId.Value, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse.SuccessResult("Successfully logged out.");
    }
}
