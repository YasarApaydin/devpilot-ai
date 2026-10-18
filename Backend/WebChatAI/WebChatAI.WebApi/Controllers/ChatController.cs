using System.Runtime.CompilerServices;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Chat;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.WebApi.Abstractions;

namespace WebChatAI.WebApi.Controllers;

[Authorize]
public class ChatController : ApiController
{
    private readonly IAiService _aiService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IUserAiKeyRepository _userAiKeyRepository;
    private readonly IApiKeyEncryptionService _encryptionService;
    private readonly IAiProviderRegistry _providerRegistry;
    private readonly ICurrentUserService _currentUserService;
    private readonly IQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMediator mediator,
        IAiService aiService,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IDocumentChunkRepository chunkRepository,
        IUserAiKeyRepository userAiKeyRepository,
        IApiKeyEncryptionService encryptionService,
        IAiProviderRegistry providerRegistry,
        ICurrentUserService currentUserService,
        IQuotaService quotaService,
        IUnitOfWork unitOfWork,
        IHostEnvironment env,
        ILogger<ChatController> logger) : base(mediator)
    {
        _aiService = aiService;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _chunkRepository = chunkRepository;
        _userAiKeyRepository = userAiKeyRepository;
        _encryptionService = encryptionService;
        _providerRegistry = providerRegistry;
        _currentUserService = currentUserService;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
        _env = env;
        _logger = logger;
    }

    [HttpPost("stream")]
    [EnableRateLimiting("HeavyWorkloadPolicy")]
    public async Task StreamChat([FromBody] StreamChatRequestDto request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Mesaj içeriği boş olamaz." }), cancellationToken);
            return;
        }

        if (request.Message.Length > 10000)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Mesaj uzunluğu izin verilen üst sınırı (10.000 karakter) aşamaz." }), cancellationToken);
            return;
        }

        var userId = _currentUserService.UserId.Value;
        var conversation = await _conversationRepository.Where(c => c.Id == request.ConversationId && c.UserId == userId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // 1. Resolve AI Provider and Validate BYOK Key prior to debiting quota (DEV-API-018)
        var provider = (!string.IsNullOrWhiteSpace(request.Provider) ? request.Provider : "system").Trim().ToLowerInvariant();
        var model = request.Model;
        string? decryptedByokKey = null;
        IAiProviderAdapter? byokAdapter = null;
        UserAiKey? userKey = null;

        if (provider != "system")
        {
            userKey = await _userAiKeyRepository.GetByUserAndProviderAsync(userId, provider, cancellationToken);
            if (userKey == null || !userKey.IsActive)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                Response.ContentType = "text/plain; charset=utf-8";
                await Response.WriteAsync($"Seçtiğiniz '{provider}' sağlayıcısı için hesabınızda kayıtlı bir API anahtarı bulunamadı. Lütfen profilinizdeki AI Modellerim bölümünden API anahtarınızı kaydedin.", cancellationToken);
                return;
            }

            byokAdapter = _providerRegistry.GetAdapter(provider);
            if (byokAdapter == null)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                Response.ContentType = "text/plain; charset=utf-8";
                await Response.WriteAsync($"'{provider}' sağlayıcısı sistem tarafından desteklenmiyor.", cancellationToken);
                return;
            }

            try
            {
                decryptedByokKey = _encryptionService.DecryptApiKey(userKey.EncryptedApiKey);
            }
            catch
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                Response.ContentType = "text/plain; charset=utf-8";
                await Response.WriteAsync("Kayıtlı API anahtarınız çözümlenemedi. Lütfen profilinizden anahtarı yeniden kaydedin.", cancellationToken);
                return;
            }
        }

        // 2. Atomic server-side quota consumption with BYOK exemption
        var quotaResult = await _quotaService.TryConsumeCreditAsync(
            userId,
            provider,
            _currentUserService.DeviceId,
            _currentUserService.IpAddress,
            cancellationToken
        );

        if (!quotaResult.Allowed)
        {
            var isSilentStreamError = Request.Headers.ContainsKey("X-Silent-Stream-Error");
            Response.StatusCode = isSilentStreamError ? StatusCodes.Status200OK : StatusCodes.Status429TooManyRequests;
            Response.ContentType = "application/json; charset=utf-8";
            Response.Headers["X-Stream-Error"] = "QUOTA_EXHAUSTED";
            Response.Headers["Access-Control-Expose-Headers"] = "X-Stream-Error, Retry-After";
            if (quotaResult.ResetAt.HasValue)
            {
                var secondsUntilReset = Math.Max(0, (int)(quotaResult.ResetAt.Value - DateTime.UtcNow).TotalSeconds);
                Response.Headers["Retry-After"] = secondsUntilReset.ToString();
            }
            var errorJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = quotaResult.RejectionReason ?? "Günlük 15 mesaj kredi limitinize ulaştınız.",
                code = "QUOTA_EXHAUSTED",
                remainingCredits = 0,
                resetAt = quotaResult.ResetAt
            });
            await Response.WriteAsync(errorJson, cancellationToken);
            return;
        }

        // 3. Save user message to database
        var userMessage = new Message
        {
            ConversationId = conversation.Id,
            UserId = userId,
            Role = "user",
            Content = request.Message,
            InputType = "text"
        };
        await _messageRepository.AddAsync(userMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Fetch the 20 most recent conversation messages chronologically (DEV-BUG-017)
        var history = (await _messageRepository.Where(m => m.ConversationId == conversation.Id && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken))
            .OrderBy(m => m.CreatedAt)
            .ToList();

        // 5. Search user's document chunks for RAG context
        List<DocumentChunk> chunks;
        if (request.DocumentId.HasValue)
        {
            chunks = await _chunkRepository.GetDocumentChunksAsync(request.DocumentId.Value, userId, topK: 10, cancellationToken);
        }
        else
        {
            chunks = await _chunkRepository.GetRelevantChunksAsync(userId, request.Message, topK: 6, cancellationToken);
        }

        var ragContext = chunks.Select(c => c.Document != null 
            ? $"[{c.Document.FileName} - Parça {c.ChunkIndex + 1}]: {c.Content}" 
            : c.Content).ToList();

        // 6. Stream AI response with BYOK / System fallback
        IAsyncEnumerable<string> streamSource;
        string modelUsed = "DevPilot Default (Qwen 3.8 / Groq)";
        string? fallbackReason = null;
        string? originalModel = null;
        int? currentRemainingCredits = quotaResult.RemainingCredits;

        if (provider != "system" && byokAdapter != null && !string.IsNullOrWhiteSpace(decryptedByokKey))
        {
            var apiKey = decryptedByokKey;
            var adapter = byokAdapter;

            var targetModel = !string.IsNullOrWhiteSpace(model) ? model : userKey?.PreferredModel ?? adapter.DefaultModels.FirstOrDefault()?.Id ?? "default";
            originalModel = $"{adapter.DisplayName} ({targetModel})";

            // Safe pre-flight test of the first chunk to catch provider 503 / 429 / 404 / timeouts before response starts
            bool providerFailed = false;
            string? firstChunk = null;
            string providerFailureReason = "";
            IAsyncEnumerator<string>? enumerator = null;

            try
            {
                var candidateStream = adapter.StreamChatAsync(history, request.Message, targetModel, apiKey, ragContext, cancellationToken);
                enumerator = candidateStream.GetAsyncEnumerator(cancellationToken);

                if (await enumerator.MoveNextAsync())
                {
                    firstChunk = enumerator.Current;
                    if (IsProviderErrorMessage(firstChunk, out var shortReason))
                    {
                        providerFailed = true;
                        providerFailureReason = shortReason;
                    }
                }
                else
                {
                    providerFailed = true;
                    providerFailureReason = "Yanıt Alınamadı";
                }
            }
            catch (Exception ex)
            {
                providerFailed = true;
                providerFailureReason = FormatShortError(ex.Message);
            }

            if (providerFailed)
            {
                if (enumerator != null)
                {
                    await enumerator.DisposeAsync();
                }

                // Deduct credit from user quota for using system model fallback.
                // If user has 0 credits, reject fallback to prevent free system model bypass!
                CreditConsumptionResult fallbackQuotaResult;
                try
                {
                    fallbackQuotaResult = await _quotaService.TryConsumeCreditAsync(
                        userId,
                        "system",
                        _currentUserService.DeviceId,
                        _currentUserService.IpAddress,
                        cancellationToken
                    );
                }
                catch (Exception quotaEx)
                {
                    _logger.LogWarning(quotaEx, "Could not debit quota on fallback for user {UserId}", userId);
                    fallbackQuotaResult = new CreditConsumptionResult(Allowed: false, IsByokBypass: false, RemainingCredits: 0, RejectionReason: "Kota kontrolü sırasında bir hata oluştu.");
                }

                if (!fallbackQuotaResult.Allowed)
                {
                    userMessage.IsDeleted = true;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var isSilentStreamError = Request.Headers.ContainsKey("X-Silent-Stream-Error");
                    Response.StatusCode = isSilentStreamError ? StatusCodes.Status200OK : StatusCodes.Status429TooManyRequests;
                    Response.ContentType = "application/json; charset=utf-8";
                    Response.Headers["X-Stream-Error"] = "QUOTA_EXHAUSTED";
                    Response.Headers["Access-Control-Expose-Headers"] = "X-Stream-Error, Retry-After";
                    if (fallbackQuotaResult.ResetAt.HasValue)
                    {
                        var secondsUntilReset = Math.Max(0, (int)(fallbackQuotaResult.ResetAt.Value - DateTime.UtcNow).TotalSeconds);
                        Response.Headers["Retry-After"] = secondsUntilReset.ToString();
                    }
                    var errorJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = $"Seçtiğiniz AI sağlayıcısı ({adapter.DisplayName}) yanıt veremedi ({providerFailureReason}). Günlük kredi limitiniz tükendiği için varsayılan sistem modeline geçiş yapılamadı.",
                        code = "QUOTA_EXHAUSTED",
                        remainingCredits = 0,
                        resetAt = fallbackQuotaResult.ResetAt,
                        originalModel = originalModel,
                        providerFailureReason = providerFailureReason
                    });
                    await Response.WriteAsync(errorJson, cancellationToken);
                    return;
                }

                currentRemainingCredits = fallbackQuotaResult.RemainingCredits;
                fallbackReason = providerFailureReason;
                modelUsed = "DevPilot Default (Qwen 3.8 / Groq)";
                conversation.Model = "system:qwen/qwen3.8-27b";

                streamSource = _aiService.StreamChatAsync(history, request.Message, "qwen/qwen3.8-27b", ragContext, cancellationToken);
            }
            else
            {
                modelUsed = $"{adapter.DisplayName} ({targetModel})";
                conversation.Model = $"{provider}:{targetModel}";
                streamSource = YieldFirstAndRest(firstChunk!, enumerator!, cancellationToken);
            }
        }
        else
        {
            modelUsed = "DevPilot Default (Qwen 3.8 / Groq)";
            conversation.Model = !string.IsNullOrWhiteSpace(model) ? model : "qwen/qwen3.8-27b";
            streamSource = _aiService.StreamChatAsync(history, request.Message, conversation.Model, ragContext, cancellationToken);
        }

        // 5. Set stream headers with model identification and fallback metadata
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["X-Model-Used"] = SanitizeHeaderValue(modelUsed);
        if (!string.IsNullOrEmpty(fallbackReason))
        {
            Response.Headers["X-Fallback-Reason"] = SanitizeHeaderValue(fallbackReason);
        }
        if (!string.IsNullOrEmpty(originalModel))
        {
            Response.Headers["X-Original-Model"] = SanitizeHeaderValue(originalModel);
        }
        if (currentRemainingCredits.HasValue)
        {
            Response.Headers["X-Remaining-Credits"] = currentRemainingCredits.Value.ToString();
        }
        Response.Headers["Access-Control-Expose-Headers"] = "X-Model-Used, X-Fallback-Reason, X-Original-Model, X-Remaining-Credits";

        var fullResponseBuilder = new StringBuilder();

        // 6. Stream LLM tokens to response stream in real-time
        try
        {
            await foreach (var chunk in streamSource)
            {
                fullResponseBuilder.Append(chunk);
                var bytes = Encoding.UTF8.GetBytes(chunk);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Stream execution interrupted. CorrelationId: {CorrelationId}", correlationId);
            var isDevelopment = _env.IsDevelopment();
            var errMessage = isDevelopment
                ? $"\n\n[İletişim Kesintisi: {ex.Message} (Takip No: {correlationId})]"
                : $"\n\n[İletişim Kesintisi: Yapay zeka servis sağlayıcısıyla iletişim sırasında beklenmeyen bir hata oluştu. (Takip No: {correlationId})]";
            fullResponseBuilder.Append(errMessage);
            var bytes = Encoding.UTF8.GetBytes(errMessage);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        // 6. Persist assistant reply to database (use CancellationToken.None so client disconnect at stream end does not drop the message)
        try
        {
            var assistantMessage = new Message
            {
                ConversationId = conversation.Id,
                UserId = null,
                Role = "assistant",
                Content = fullResponseBuilder.ToString(),
                InputType = "text",
                Model = modelUsed,
                FallbackReason = fallbackReason,
                OriginalModel = originalModel
            };
            await _messageRepository.AddAsync(assistantMessage, CancellationToken.None);

            conversation.LastMessageAt = DateTime.UtcNow;
            _conversationRepository.Update(conversation);

            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist assistant reply for conversation {ConversationId}", conversation.Id);
        }
    }

    [HttpGet("credits")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetCredits(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        var dto = await _quotaService.GetQuotaStatusAsync(_currentUserService.UserId.Value, cancellationToken);
        return Ok(ApiResponse<UserCreditDto>.Ok(dto));
    }

    [HttpPost("voice")]
    [EnableRateLimiting("HeavyWorkloadPolicy")]
    public async Task<IActionResult> VoiceChat([FromForm] Guid conversationId, [FromForm] IFormFile? audioFile, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(ApiResponse<VoiceChatResponseDto>.Fail("Ses dosyası gereklidir."));
        }

        const long maxFileSize = 25 * 1024 * 1024; // 25 MB
        if (audioFile.Length > maxFileSize)
        {
            return BadRequest(ApiResponse<VoiceChatResponseDto>.Fail("Ses dosyası boyutu 25 MB'tan büyük olamaz."));
        }

        var allowedExtensions = new[] { ".wav", ".mp3", ".m4a", ".ogg", ".webm", ".flac", ".aac" };
        var extension = Path.GetExtension(audioFile.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<VoiceChatResponseDto>.Fail("Desteklenmeyen ses formatı. Lütfen WAV, MP3, M4A, OGG, WEBM, FLAC veya AAC formatında bir dosya yükleyin."));
        }

        await using var audioStream = audioFile.OpenReadStream();
        var command = new SendVoiceMessageCommand(conversationId, audioStream, audioFile.FileName);
        var result = await Mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Message?.Contains("limit", StringComparison.OrdinalIgnoreCase) == true)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    private static async IAsyncEnumerable<string> YieldFirstAndRest(
        string first,
        IAsyncEnumerator<string> enumerator,
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return first;
        while (!ct.IsCancellationRequested && await enumerator.MoveNextAsync())
        {
            yield return enumerator.Current;
        }
        await enumerator.DisposeAsync();
    }

    private static bool IsProviderErrorMessage(string chunk, out string shortReason)
    {
        shortReason = "Sağlayıcı Hatası";
        if (string.IsNullOrWhiteSpace(chunk)) return false;

        var text = chunk.Trim();
        if (!text.StartsWith("Hata:") &&
            !text.StartsWith("Bilgi:") &&
            !text.Contains("API Hatası") &&
            !text.Contains("[API Error") &&
            !text.Contains("RESOURCE_EXHAUSTED") &&
            !text.Contains("UNAVAILABLE"))
        {
            return false;
        }

        if (text.Contains("Workspace", StringComparison.OrdinalIgnoreCase) || text.Contains("workspace", StringComparison.OrdinalIgnoreCase))
        {
            shortReason = "Workspace Gerekli (400)";
            return true;
        }
        if (text.Contains("402") || text.Contains("bakiye", StringComparison.OrdinalIgnoreCase) || text.Contains("Insufficient Balance", StringComparison.OrdinalIgnoreCase))
        {
            shortReason = "Bakiye Yetersiz (402)";
            return true;
        }
        if (text.Contains("503") || text.Contains("UNAVAILABLE") || text.Contains("high demand") || text.Contains("yüksek talep"))
        {
            shortReason = "Sunucu Yoğun (503)";
            return true;
        }
        if (text.Contains("429") || text.Contains("RESOURCE_EXHAUSTED") || text.Contains("kota") || text.Contains("hız limit"))
        {
            shortReason = "Kota Doldu (429)";
            return true;
        }
        if (text.Contains("401") || text.Contains("403") || text.Contains("geçersiz") || text.Contains("API_KEY_INVALID"))
        {
            shortReason = "Anahtar Geçersiz";
            return true;
        }
        if (text.Contains("404") || text.Contains("model_decommissioned") || text.Contains("model_not_found") || text.Contains("does not exist"))
        {
            shortReason = "Model Bulunamadı";
            return true;
        }
        if (text.Contains("zaman aşımı") || text.Contains("Timeout") || text.Contains("408"))
        {
            shortReason = "Zaman Aşımı";
            return true;
        }

        shortReason = "Sağlayıcı Hatası";
        return true;
    }

    private static string FormatShortError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Bağlantı Hatası";
        if (message.Contains("Workspace", StringComparison.OrdinalIgnoreCase) || message.Contains("workspace", StringComparison.OrdinalIgnoreCase))
            return "Workspace Gerekli (400)";
        if (message.Contains("402") || message.Contains("bakiye", StringComparison.OrdinalIgnoreCase) || message.Contains("Insufficient Balance", StringComparison.OrdinalIgnoreCase))
            return "Bakiye Yetersiz (402)";
        if (message.Contains("Timeout", StringComparison.OrdinalIgnoreCase) || message.Contains("zaman aşımı", StringComparison.OrdinalIgnoreCase))
            return "Zaman Aşımı";
        if (message.Contains("503") || message.Contains("high demand", StringComparison.OrdinalIgnoreCase))
            return "Sunucu Yoğun (503)";
        if (message.Contains("429") || message.Contains("quota", StringComparison.OrdinalIgnoreCase))
            return "Kota Doldu (429)";
        if (message.Contains("401") || message.Contains("403"))
            return "Anahtar Geçersiz";
        return "Bağlantı Hatası";
    }

    private static string SanitizeHeaderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        // Escape non-ASCII and special characters to guarantee valid HTTP/1.1 and Kestrel headers
        return Uri.EscapeDataString(value.Trim());
    }
}
