using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class UserAiKey : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string Provider { get; set; } = default!;
    public string EncryptedApiKey { get; set; } = default!;
    public string KeyHint { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public bool IsValid { get; set; } = true;
    public DateTime? LastTestedAt { get; set; }
    public string? PreferredModel { get; set; }
}
