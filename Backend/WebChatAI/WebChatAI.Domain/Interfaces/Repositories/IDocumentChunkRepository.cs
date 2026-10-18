using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IDocumentChunkRepository : IRepository<DocumentChunk>
{
    Task<List<DocumentChunk>> GetRelevantChunksAsync(Guid userId, string query, int topK = 5, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, Guid userId, int topK = 10, CancellationToken cancellationToken = default);
}
