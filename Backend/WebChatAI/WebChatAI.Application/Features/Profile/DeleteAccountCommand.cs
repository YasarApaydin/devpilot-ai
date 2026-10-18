using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Profile;

public sealed record DeleteAccountCommand : IRequest<ApiResponse>;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, ApiResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse.FailureResult("Yetkisiz işlem.");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse.FailureResult("Kullanıcı bulunamadı.");
        }

        await _userRepository.HardDeleteUserAsync(userId, cancellationToken);

        return ApiResponse.SuccessResult("Hesabınız ve tüm ilişkili verileriniz kalıcı olarak silindi.");
    }
}
