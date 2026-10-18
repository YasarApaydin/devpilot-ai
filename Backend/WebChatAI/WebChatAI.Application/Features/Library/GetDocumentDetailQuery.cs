using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Library;

public sealed record GetDocumentDetailQuery(Guid Id) : IRequest<ApiResponse<DocumentDetailDto>>;

public sealed class GetDocumentDetailQueryHandler : IRequestHandler<GetDocumentDetailQuery, ApiResponse<DocumentDetailDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentDetailQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentChunkRepository chunkRepository,
        ICurrentUserService currentUserService)
    {
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<DocumentDetailDto>> Handle(GetDocumentDetailQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<DocumentDetailDto>.Fail("Yetkisiz işlem.");
        }

        var userId = _currentUserService.UserId.Value;
        var document = await _documentRepository.Where(d => d.Id == request.Id && d.UserId == userId && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            return ApiResponse<DocumentDetailDto>.Fail("Doküman bulunamadı.");
        }

        var chunks = await _chunkRepository.Where(c => c.DocumentId == document.Id && !c.IsDeleted)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

        var chunkDtos = chunks.Select(c => new DocumentChunkDto(
            c.Id,
            c.ChunkIndex,
            c.Content,
            c.TokenCount
        )).ToList();

        var fullContent = string.Join("\n\n", chunks.Select(c => c.Content));

        var detailDto = new DocumentDetailDto(
            document.Id,
            document.FileName,
            document.FileSizeBytes,
            document.ProcessingStatus,
            document.ContentType,
            document.CreatedAt,
            document.Summary,
            document.TotalTokens ?? 0,
            chunkDtos,
            fullContent
        );

        return ApiResponse<DocumentDetailDto>.Ok(detailDto, "Doküman detayları başarıyla getirildi.");
    }
}
