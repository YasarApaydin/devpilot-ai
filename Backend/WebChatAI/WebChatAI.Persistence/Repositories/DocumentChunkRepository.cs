using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class DocumentChunkRepository : Repository<DocumentChunk>, IDocumentChunkRepository
{
    public DocumentChunkRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, Guid userId, int topK = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Document)
            .Where(c => c.DocumentId == documentId && c.Document.UserId == userId && !c.Document.IsDeleted && !c.IsDeleted)
            .OrderBy(c => c.ChunkIndex)
            .Take(topK)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DocumentChunk>> GetRelevantChunksAsync(Guid userId, string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<DocumentChunk>();
        }

        var terms = query.Split(new[] { ' ', ',', '.', '?', '!', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Select(t => t.ToLower())
            .Distinct()
            .Take(10)
            .ToList();

        // Load user's chunks with document metadata
        var userChunks = await _dbSet
            .Include(c => c.Document)
            .Where(c => c.Document.UserId == userId && !c.Document.IsDeleted && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (userChunks.Count == 0)
        {
            return new List<DocumentChunk>();
        }

        if (terms.Count == 0)
        {
            return userChunks.Take(topK).ToList();
        }

        // 1. Try finding chunks by term occurrences
        var ranked = userChunks
            .Select(c => new
            {
                Chunk = c,
                Score = terms.Count(term => c.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        if (ranked.Count > 0)
        {
            return ranked;
        }

        // 2. Check if a document filename is mentioned in the query
        var matchedByFileName = userChunks
            .Where(c => c.Document != null && !string.IsNullOrWhiteSpace(c.Document.FileName) &&
                        query.Contains(c.Document.FileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.ChunkIndex)
            .Take(topK)
            .ToList();

        if (matchedByFileName.Count > 0)
        {
            return matchedByFileName;
        }

        // 3. Fallback: If user asks generally to inspect/analyze their documents
        var inspectionKeywords = new[] { "doküman", "dokuman", "belge", "dosya", "kütüphane", "kutuphane", "pdf", "incele", "özet", "ozet", "analiz", "oku" };
        var queryLower = query.ToLowerInvariant();
        if (inspectionKeywords.Any(k => queryLower.Contains(k)))
        {
            return userChunks.Take(topK).ToList();
        }

        return new List<DocumentChunk>();
    }
}
