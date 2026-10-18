using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class Document : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = default!;
    public string? Summary { get; set; }
    public int? TotalTokens { get; set; }
    public string ProcessingStatus { get; set; } = "Ready"; // 'Pending', 'Processing', 'Completed', 'Failed', 'Ready'

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
