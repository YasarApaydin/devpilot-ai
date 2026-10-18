using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;

    public int ChunkIndex { get; set; }
    public string Content { get; set; } = default!;
    public int? TokenCount { get; set; }
}
