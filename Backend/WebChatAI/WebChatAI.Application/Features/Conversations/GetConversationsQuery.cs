using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record GetConversationsQuery(int Page = 1, int PageSize = 50) : IRequest<ApiResponse<List<ConversationDto>>>;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, ApiResponse<List<ConversationDto>>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationsQueryHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<List<ConversationDto>>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<List<ConversationDto>>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var conversations = await _conversationRepository.Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.PinnedAt)
            .ThenByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationDto(
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                c.Messages.Count(m => !m.IsDeleted),
                c.IsPinned,
                c.PinnedAt
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<ConversationDto>>.Ok(conversations);
    }
}
