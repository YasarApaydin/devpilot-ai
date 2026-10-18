using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Model { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public bool IsPinned { get; set; } = false;
    public DateTime? PinnedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
