using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Library;

public sealed record GetDocumentsQuery(string? Search = null, int Page = 1, int PageSize = 50) : IRequest<ApiResponse<List<LibraryDocumentDto>>>;

public sealed class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, ApiResponse<List<LibraryDocumentDto>>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        ICurrentUserService currentUserService)
    {
        _documentRepository = documentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<List<LibraryDocumentDto>>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<List<LibraryDocumentDto>>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var query = _documentRepository.Where(d => d.UserId == userId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(d => d.FileName.ToLower().Contains(search) || (d.Summary != null && d.Summary.ToLower().Contains(search)));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var documents = await query.OrderByDescending(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new LibraryDocumentDto(
                d.Id,
                d.FileName,
                d.FileSizeBytes,
                d.ProcessingStatus,
                d.ContentType,
                d.CreatedAt,
                d.UpdatedAt,
                null,
                d.Summary
            ))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<LibraryDocumentDto>>.Ok(documents);
    }
}
