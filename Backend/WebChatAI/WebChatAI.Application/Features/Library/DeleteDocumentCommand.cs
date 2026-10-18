using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Library;

public sealed record DeleteDocumentCommand(Guid Id) : IRequest<ApiResponse>;

public sealed class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, ApiResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse.FailureResult("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var document = await _documentRepository.Where(d => d.Id == request.Id && d.UserId == userId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            return ApiResponse.FailureResult("Document not found.");
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = userId;
        _documentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Document deleted successfully.");
    }
}
