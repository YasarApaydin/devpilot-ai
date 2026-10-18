namespace WebChatAI.Application.DTOs;

public record LibraryDocumentDto(
    Guid Id,
    string FileName,
    long Size,
    string Status,
    string FileType,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? DownloadUrl = null,
    string? Summary = null
);

public record UploadDocumentResponseDto(
    LibraryDocumentDto Document,
    string? Message = null
);

public record DocumentChunkDto(
    Guid Id,
    int ChunkIndex,
    string Content,
    int? TokenCount
);

public record DocumentDetailDto(
    Guid Id,
    string FileName,
    long Size,
    string Status,
    string FileType,
    DateTime CreatedAt,
    string? Summary,
    int TotalTokens,
    List<DocumentChunkDto> Chunks,
    string FullContent
);

public record UpdateProfileRequestDto(
    string FirstName,
    string LastName
);

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword
);
