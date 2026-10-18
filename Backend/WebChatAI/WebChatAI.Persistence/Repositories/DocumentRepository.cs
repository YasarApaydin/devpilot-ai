using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Document>> GetByUserIdAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(d => d.UserId == userId && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.FileName.ToLower().Contains(s) || (d.Summary != null && d.Summary.ToLower().Contains(s)));
        }

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetWithChunksAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.Id == id && d.UserId == userId && !d.IsDeleted)
            .Include(d => d.Chunks.OrderBy(c => c.ChunkIndex))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
