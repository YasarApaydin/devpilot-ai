using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Chat;

public sealed record SendVoiceMessageCommand(
    Guid ConversationId,
    Stream AudioStream,
    string FileName
) : IRequest<ApiResponse<VoiceChatResponseDto>>;

public sealed class SendVoiceMessageCommandHandler : IRequestHandler<SendVoiceMessageCommand, ApiResponse<VoiceChatResponseDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IVoiceService _voiceService;
    private readonly IAiService _aiService;
    private readonly IQuotaService _quotaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendVoiceMessageCommandHandler> _logger;

    public SendVoiceMessageCommandHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IDocumentChunkRepository chunkRepository,
        IVoiceService voiceService,
        IAiService aiService,
        IQuotaService quotaService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<SendVoiceMessageCommandHandler>? logger = null)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _chunkRepository = chunkRepository;
        _voiceService = voiceService;
        _aiService = aiService;
        _quotaService = quotaService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<SendVoiceMessageCommandHandler>.Instance;
    }

    public async Task<ApiResponse<VoiceChatResponseDto>> Handle(SendVoiceMessageCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<VoiceChatResponseDto>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.ConversationId && c.UserId == userId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            return ApiResponse<VoiceChatResponseDto>.Fail("Conversation not found.");
        }

        // 1. Enforce Quota Consumption BEFORE expensive audio transcription
        var quotaResult = await _quotaService.TryConsumeCreditAsync(
            userId,
            provider: "system",
            _currentUserService.DeviceId,
            _currentUserService.IpAddress,
            cancellationToken
        );

        if (!quotaResult.Allowed)
        {
            return ApiResponse<VoiceChatResponseDto>.Fail(quotaResult.RejectionReason ?? "Günlük mesaj limitinize ulaştınız.");
        }

        var creditConsumed = !quotaResult.IsByokBypass;
        int refundState = 0; // 0 = not refunded, 1 = refunded
        bool refundSucceeded = false;

        async Task<bool> TryRefundAsync()
        {
            if (creditConsumed && Interlocked.Exchange(ref refundState, 1) == 0)
            {
                try
                {
                    refundSucceeded = await _quotaService.RefundCreditAsync(userId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refund credit for user {UserId} after failed voice message.", userId);
                    refundSucceeded = false;
                }
            }
            return refundSucceeded;
        }

        try
        {
            // 2. Transcribe audio via Whisper
            var transcription = await _voiceService.TranscribeAudioAsync(request.AudioStream, request.FileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(transcription))
            {
                var refunded = await TryRefundAsync();
                var msg = (creditConsumed && refunded)
                    ? "Ses kaydı çözümlenemedi. Krediniz iade edildi. Lütfen net bir konuşma ile tekrar deneyin."
                    : (creditConsumed && !refunded)
                        ? "Ses kaydı çözümlenemedi. Kredi iadesi yapılamadı, lütfen destek ekibiyle iletişime geçin."
                        : "Ses kaydı çözümlenemedi. Lütfen net bir konuşma ile tekrar deneyin.";
                return ApiResponse<VoiceChatResponseDto>.Fail(msg);
            }

            // 3. Save user message
            var userMessage = new Message
            {
                ConversationId = conversation.Id,
                UserId = userId,
                Role = "user",
                Content = transcription,
                InputType = "voice"
            };
            await _messageRepository.AddAsync(userMessage, cancellationToken);

            // 4. Retrieve the 20 most recent conversation messages chronologically (DEV-BUG-017)
            var history = (await _messageRepository.Where(m => m.ConversationId == conversation.Id && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken))
                .OrderBy(m => m.CreatedAt)
                .ToList();

            var chunks = await _chunkRepository.GetRelevantChunksAsync(userId, transcription, topK: 3, cancellationToken);
            var ragContext = chunks.Select(c => c.Content).ToList();

            // 5. Generate AI response
            string replyContent;
            try
            {
                replyContent = await _aiService.GenerateChatResponseAsync(
                    history,
                    transcription,
                    conversation.Model,
                    ragContext,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI response generation failed for conversation {ConversationId}", conversation.Id);
                var refunded = await TryRefundAsync();
                var msg = (creditConsumed && refunded)
                    ? "Yapay zeka yanıtı üretilemedi. Krediniz iade edildi. Lütfen tekrar deneyin."
                    : (creditConsumed && !refunded)
                        ? "Yapay zeka yanıtı üretilemedi. Kredi iadesi yapılamadı, lütfen destek ekibiyle iletişime geçin."
                        : "Yapay zeka yanıtı üretilemedi. Lütfen tekrar deneyin.";
                return ApiResponse<VoiceChatResponseDto>.Fail(msg);
            }

            // 6. Save assistant message
            var assistantMessage = new Message
            {
                ConversationId = conversation.Id,
                UserId = null,
                Role = "assistant",
                Content = replyContent,
                InputType = "text"
            };
            await _messageRepository.AddAsync(assistantMessage, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            _conversationRepository.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<VoiceChatResponseDto>.Ok(
                new VoiceChatResponseDto(
                    assistantMessage.Id,
                    transcription,
                    replyContent,
                    null
                ),
                "Voice message processed."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing voice message for user {UserId}", userId);
            var refunded = await TryRefundAsync();
            var msg = (creditConsumed && refunded)
                ? "Sesli mesaj işlenirken bir hata oluştu. Krediniz iade edildi."
                : (creditConsumed && !refunded)
                    ? "Sesli mesaj işlenirken bir hata oluştu. Kredi iadesi yapılamadı, lütfen destek ekibiyle iletişime geçin."
                    : "Sesli mesaj işlenirken bir hata oluştu. Lütfen tekrar deneyin.";
            return ApiResponse<VoiceChatResponseDto>.Fail(msg);
        }
    }
}
