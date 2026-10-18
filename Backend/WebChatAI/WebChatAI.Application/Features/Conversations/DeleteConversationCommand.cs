using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record DeleteConversationCommand(Guid Id) : IRequest<ApiResponse>;

public sealed class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand, ApiResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse.FailureResult("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.Id && c.UserId == userId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            return ApiResponse.FailureResult("Conversation not found.");
        }

        conversation.IsDeleted = true;
        conversation.DeletedAt = DateTime.UtcNow;
        conversation.DeletedBy = userId;
        _conversationRepository.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Conversation deleted successfully.");
    }
}
