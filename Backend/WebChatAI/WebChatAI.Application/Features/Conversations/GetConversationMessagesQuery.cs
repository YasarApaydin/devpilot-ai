using MediatR;
using Microsoft.EntityFrameworkCore;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Application.Features.Conversations;

public sealed record GetConversationMessagesQuery(Guid ConversationId, int Page = 1, int PageSize = 500) : IRequest<ApiResponse<List<ChatMessageDto>>>;

public sealed class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, ApiResponse<List<ChatMessageDto>>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISecurityScanRepository _scanRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ISecurityScanRepository scanRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _scanRepository = scanRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<List<ChatMessageDto>>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return ApiResponse<List<ChatMessageDto>>.Fail("Unauthorized");
        }

        var userId = _currentUserService.UserId.Value;
        var conversationExists = await _conversationRepository.AnyAsync(
            c => c.Id == request.ConversationId && c.UserId == userId && !c.IsDeleted,
            cancellationToken
        );

        if (!conversationExists)
        {
            return ApiResponse<List<ChatMessageDto>>.Fail("Conversation not found.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 1000);

        // Fetch the most recent messages for the requested page, then re-sort chronologically (CreatedAt ASC)
        var messages = (await _messageRepository.Where(m => m.ConversationId == request.ConversationId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToList();

        // Only query scans and their full findings when assistant messages on the current page actually reference a security scan
        var assistantMessages = messages.Where(m => m.Role == "assistant").ToList();
        var needsScans = assistantMessages.Any(m =>
            m.Content.Contains("güvenlik taraması", StringComparison.OrdinalIgnoreCase) ||
            m.Content.Contains("Security Engine", StringComparison.OrdinalIgnoreCase) ||
            m.Content.Contains("Pasif Dış Güvenlik", StringComparison.OrdinalIgnoreCase) ||
            m.Content.Contains("🛡️") ||
            m.Content.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
            m.Content.Contains("https://", StringComparison.OrdinalIgnoreCase));

        var scans = new List<SecurityScan>();
        if (needsScans)
        {
            var candidates = await _scanRepository.Where(s => s.ConversationId == request.ConversationId && !s.IsDeleted)
                .Select(s => new { s.Id, s.TargetUrl, s.NormalizedTargetUrl, s.CreatedAt })
                .ToListAsync(cancellationToken);

            var pageScanIds = new HashSet<Guid>();
            foreach (var m in assistantMessages)
            {
                var match = candidates.FirstOrDefault(s => !pageScanIds.Contains(s.Id) &&
                                (m.Content.Contains(s.TargetUrl) || m.Content.Contains(s.NormalizedTargetUrl)))
                            ?? candidates.FirstOrDefault(s => !pageScanIds.Contains(s.Id) && (m.CreatedAt - s.CreatedAt).Duration() < TimeSpan.FromSeconds(15));
                if (match != null)
                {
                    pageScanIds.Add(match.Id);
                }
            }

            if (pageScanIds.Count > 0)
            {
                scans = await _scanRepository.Where(s => pageScanIds.Contains(s.Id))
                    .Include(s => s.Findings)
                    .Include(s => s.Consent)
                    .ToListAsync(cancellationToken);
            }
        }

        var result = new List<ChatMessageDto>();
        var attachedScanIds = new HashSet<Guid>();

        foreach (var m in messages)
        {
            SecurityScanResponseDto? matchedScanDto = null;
            if (m.Role == "assistant" && scans.Count > 0)
            {
                var matchingScan = scans.FirstOrDefault(s => !attachedScanIds.Contains(s.Id) && m.Content.Contains(s.TargetUrl))
                                   ?? scans.FirstOrDefault(s => !attachedScanIds.Contains(s.Id) && (m.CreatedAt - s.CreatedAt).Duration() < TimeSpan.FromSeconds(15));

                if (matchingScan != null)
                {
                    attachedScanIds.Add(matchingScan.Id);
                    matchedScanDto = MapToScanDto(matchingScan);
                }
            }

            result.Add(new ChatMessageDto(
                m.Id,
                m.ConversationId,
                m.Role,
                m.Content,
                m.CreatedAt,
                m.InputType,
                null,
                false,
                false,
                matchedScanDto,
                m.Model,
                m.FallbackReason,
                m.OriginalModel
            ));
        }

        return ApiResponse<List<ChatMessageDto>>.Ok(result);
    }

    private static SecurityScanResponseDto MapToScanDto(WebChatAI.Domain.Entities.SecurityScan scan)
    {
        return new SecurityScanResponseDto
        {
            Id = scan.Id,
            UserId = scan.UserId,
            ConversationId = scan.ConversationId,
            TargetUrl = scan.TargetUrl,
            NormalizedTargetUrl = scan.NormalizedTargetUrl,
            ScanMode = scan.ScanMode,
            SelectedCategories = scan.SelectedCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            CustomInstructions = scan.CustomInstructions,
            Status = scan.Status,
            CreatedAt = scan.CreatedAt,
            StartedAt = scan.StartedAt,
            CompletedAt = scan.CompletedAt,
            ErrorMessage = scan.ErrorMessage,
            Summary = scan.Summary,
            TotalFindingsCount = scan.TotalFindingsCount,
            CriticalCount = scan.CriticalCount,
            HighCount = scan.HighCount,
            MediumCount = scan.MediumCount,
            LowCount = scan.LowCount,
            InfoCount = scan.InfoCount,
            StrixRunId = scan.StrixRunId,
            Consent = scan.Consent != null ? new SecurityScanConsentDto
            {
                Id = scan.Consent.Id,
                SecurityScanId = scan.Consent.SecurityScanId,
                TargetUrl = scan.Consent.TargetUrl,
                DisclaimerVersion = scan.Consent.DisclaimerVersion,
                HasOwnershipAuthorization = scan.Consent.HasOwnershipAuthorization,
                HasRiskAcknowledged = scan.Consent.HasRiskAcknowledged,
                ConsentedAt = scan.Consent.ConsentedAt,
                ClientIp = scan.Consent.ClientIp
            } : null,
            Findings = scan.Findings.Select(f => new SecurityScanFindingDto
            {
                Id = f.Id,
                SecurityScanId = f.SecurityScanId,
                Title = f.Title,
                Severity = f.Severity,
                Cwe = f.Cwe,
                CvssScore = f.CvssScore,
                CvssVector = f.CvssVector,
                TargetUrl = f.TargetUrl,
                Endpoint = f.Endpoint,
                Description = f.Description,
                Impact = f.Impact,
                TechnicalAnalysis = f.TechnicalAnalysis,
                Remediation = f.Remediation,
                Evidence = f.Evidence,
                PoCDescription = f.PoCDescription,
                Confidence = f.Confidence
            }).ToList()
        };
    }
}
