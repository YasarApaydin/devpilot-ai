using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record CreateConversationCommand(string? Title = null) : IRequest<ApiResponse<ConversationDto>>;

public sealed class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<ConversationDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var title = string.IsNullOrWhiteSpace(request.Title) ? "New Conversation" : request.Title.Trim();

        var conversation = new Conversation
        {
            UserId = userId,
            Title = title,
            Model = "openai/gpt-oss-120b",
            LastMessageAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            0,
            conversation.IsPinned,
            conversation.PinnedAt
        );

        return ApiResponse<ConversationDto>.Ok(dto, "Conversation created successfully.");
    }
}
