using System.ComponentModel.DataAnnotations;

namespace WebChatAI.Application.DTOs;

public record ConversationDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int MessageCount,
    bool IsPinned = false,
    DateTime? PinnedAt = null
);

public record CreateConversationRequestDto(
    string? Title
);

public record UpdateConversationRequestDto(
    string? Title,
    bool? IsPinned = null
);

public record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    string Role,
    string Content,
    DateTime CreatedAt,
    string? InputType = "text",
    string? AudioUrl = null,
    bool? HasPdf = false,
    bool? IsStreaming = false,
    SecurityScanResponseDto? SecurityScan = null,
    string? ModelUsed = null,
    string? FallbackReason = null,
    string? OriginalModel = null
);

public record StreamChatRequestDto
{
    public Guid ConversationId { get; init; }

    [Required]
    [MaxLength(10000, ErrorMessage = "Mesaj uzunluğu izin verilen üst sınırı (10.000 karakter) aşamaz.")]
    public string Message { get; init; } = string.Empty;

    public Guid? DocumentId { get; init; }
    public string? Provider { get; init; }
    public string? Model { get; init; }

    public StreamChatRequestDto() { }

    public StreamChatRequestDto(
        Guid conversationId,
        string message,
        Guid? documentId = null,
        string? provider = null,
        string? model = null)
    {
        ConversationId = conversationId;
        Message = message;
        DocumentId = documentId;
        Provider = provider;
        Model = model;
    }

    public void Deconstruct(
        out Guid conversationId,
        out string message,
        out Guid? documentId,
        out string? provider,
        out string? model)
    {
        conversationId = ConversationId;
        message = Message;
        documentId = DocumentId;
        provider = Provider;
        model = Model;
    }
}

public record VoiceChatResponseDto(
    Guid MessageId,
    string Transcription,
    string ReplyContent,
    string? AudioUrl = null
);

public record UserCreditDto(
    string Email,
    int TotalCredits,
    int UsedCredits,
    int RemainingCredits,
    DateTime? NextResetAt,
    string? SubscriptionTier = "free",
    bool? IsByokActive = false
);
