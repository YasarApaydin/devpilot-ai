using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<List<Document>> GetByUserIdAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default);
    Task<Document?> GetWithChunksAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
