using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record GetConversationByIdQuery(Guid Id) : IRequest<ApiResponse<ConversationDto>>;

public sealed class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationByIdQueryHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<ConversationDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.Id && c.UserId == userId && !c.IsDeleted)
            .Select(c => new ConversationDto(
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                c.Messages.Count(m => !m.IsDeleted),
                c.IsPinned,
                c.PinnedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            return ApiResponse<ConversationDto>.Fail("Conversation not found.");
        }

        return ApiResponse<ConversationDto>.Ok(conversation);
    }
}
