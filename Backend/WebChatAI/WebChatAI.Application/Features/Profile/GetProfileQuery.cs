using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Profile;

public sealed record GetProfileQuery : IRequest<ApiResponse<UserDto>>;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ApiResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<UserDto>.Fail("Unauthorized");
        }

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserDto>.Fail("User profile not found.");
        }

        var dto = new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailVerified,
            null,
            user.CreatedAt
        );

        return ApiResponse<UserDto>.Ok(dto);
    }
}
