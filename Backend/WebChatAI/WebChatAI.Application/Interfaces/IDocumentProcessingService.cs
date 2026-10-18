namespace WebChatAI.Application.Interfaces;

public record DocumentProcessingResult(
    string ExtractedText,
    string Summary,
    int TotalTokens,
    List<string> Chunks,
    bool Success = true,
    string Status = "Processed",
    string? ErrorMessage = null);

public interface IDocumentProcessingService
{
    Task<DocumentProcessingResult> ProcessDocumentAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
