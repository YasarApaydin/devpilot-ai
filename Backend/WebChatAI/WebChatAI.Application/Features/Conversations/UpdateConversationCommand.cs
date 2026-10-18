using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record UpdateConversationCommand(Guid Id, string? Title = null, bool? IsPinned = null) : IRequest<ApiResponse<ConversationDto>>;

public sealed class UpdateConversationCommandValidator : AbstractValidator<UpdateConversationCommand>
{
    public UpdateConversationCommandValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Title) || x.IsPinned.HasValue)
            .WithMessage("Title or IsPinned must be provided.");
    }
}

public sealed class UpdateConversationCommandHandler : IRequestHandler<UpdateConversationCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(UpdateConversationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<ConversationDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.Id && c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            return ApiResponse<ConversationDto>.Fail("Conversation not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            conversation.Title = request.Title.Trim();
        }

        if (request.IsPinned.HasValue)
        {
            conversation.IsPinned = request.IsPinned.Value;
            conversation.PinnedAt = request.IsPinned.Value ? DateTime.UtcNow : null;
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages.Count(m => !m.IsDeleted),
            conversation.IsPinned,
            conversation.PinnedAt
        );

        return ApiResponse<ConversationDto>.Ok(dto, "Conversation updated successfully.");
    }
}

public sealed record TogglePinConversationCommand(Guid Id) : IRequest<ApiResponse<ConversationDto>>;

public sealed class TogglePinConversationCommandHandler : IRequestHandler<TogglePinConversationCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public TogglePinConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(TogglePinConversationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<ConversationDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.Id && c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            return ApiResponse<ConversationDto>.Fail("Conversation not found.");
        }

        conversation.IsPinned = !conversation.IsPinned;
        conversation.PinnedAt = conversation.IsPinned ? DateTime.UtcNow : null;
        conversation.UpdatedAt = DateTime.UtcNow;

        _conversationRepository.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages.Count(m => !m.IsDeleted),
            conversation.IsPinned,
            conversation.PinnedAt
        );

        return ApiResponse<ConversationDto>.Ok(dto, conversation.IsPinned ? "Conversation pinned." : "Conversation unpinned.");
    }
}
