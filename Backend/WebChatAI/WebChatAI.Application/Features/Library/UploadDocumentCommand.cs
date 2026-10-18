using MediatR;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Library;

public sealed record UploadDocumentCommand(
    Stream FileStream,
    string FileName,
    long FileSize,
    string ContentType
) : IRequest<ApiResponse<UploadDocumentResponseDto>>;

public sealed class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, ApiResponse<UploadDocumentResponseDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentChunkRepository chunkRepository,
        IDocumentProcessingService documentProcessingService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _documentProcessingService = documentProcessingService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UploadDocumentResponseDto>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<UploadDocumentResponseDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;

        // Process document (extract text and chunks)
        var processResult = await _documentProcessingService.ProcessDocumentAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken
        );

        var document = new Document
        {
            UserId = userId,
            FileName = request.FileName,
            FileSizeBytes = request.FileSize,
            ContentType = request.ContentType,
            Summary = processResult.Summary,
            TotalTokens = processResult.TotalTokens,
            ProcessingStatus = processResult.Status
        };

        await _documentRepository.AddAsync(document, cancellationToken);

        // Add chunks only if processing was successful and chunks exist
        if (processResult.Success && processResult.Chunks != null && processResult.Chunks.Count > 0)
        {
            for (int i = 0; i < processResult.Chunks.Count; i++)
            {
                var chunk = new DocumentChunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = i,
                    Content = processResult.Chunks[i],
                    TokenCount = processResult.Chunks[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                };
                await _chunkRepository.AddAsync(chunk, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var docDto = new LibraryDocumentDto(
            document.Id,
            document.FileName,
            document.FileSizeBytes,
            document.ProcessingStatus,
            document.ContentType,
            document.CreatedAt,
            document.UpdatedAt,
            null,
            document.Summary
        );

        return ApiResponse<UploadDocumentResponseDto>.Ok(
            new UploadDocumentResponseDto(
                docDto,
                processResult.Success
                    ? "Document uploaded and indexed successfully."
                    : (processResult.ErrorMessage ?? "Document uploaded but text could not be extracted.")
            ),
            processResult.Success ? "Upload complete." : (processResult.ErrorMessage ?? "Extraction failed.")
        );
    }
}
