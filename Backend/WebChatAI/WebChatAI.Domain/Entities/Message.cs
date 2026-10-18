using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Role { get; set; } = default!; // 'user', 'assistant', 'system', 'tool'
    public string Content { get; set; } = default!;
    public string InputType { get; set; } = "text"; // 'text', 'voice'

    public string? Model { get; set; }
    public string? FallbackReason { get; set; }
    public string? OriginalModel { get; set; }
}
