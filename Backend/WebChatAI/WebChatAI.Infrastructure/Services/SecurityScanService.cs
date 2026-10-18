using WebChatAI.Application.DTOs;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Infrastructure.Services;

public class SecurityScanService : ISecurityScanService
{
    private readonly ISecurityScanRepository _scanRepository;
    private readonly ISecurityScanConsentRepository _consentRepository;
    private readonly ISecurityScanFindingRepository _findingRepository;
    private readonly ISsrfProtectionService _ssrfProtectionService;
    private readonly ISecurityScanRunnerService _runnerService;
    private readonly ISecurityScanPdfService _pdfService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly List<ScanCategoryDto> SupportedCategories = new()
    {
        new ScanCategoryDto
        {
            Id = "security_headers",
            Title = "HTTP Güvenlik Başlıkları ve İzolasyon Politikaları",
            Description = "CSP (unsafe-inline/eval/wildcard), X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy ve COOP/CORP başlıklarının canlı analizi.",
            Cwes = new List<string> { "CWE-16", "CWE-693", "CWE-1021", "CWE-430" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "ssl_tls",
            Title = "SSL/TLS, HTTPS Zorunluluğu ve HSTS Taşıma Güvenliği",
            Description = "HTTPS şifreleme durumu, HSTS (Strict-Transport-Security) max-age süresi, alt alan adı koruması ve sayfa içi karma içerik (Mixed Content) testi.",
            Cwes = new List<string> { "CWE-319", "CWE-295", "CWE-523" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "cookie_cors_security",
            Title = "Çerez (Cookie) Güvenlik Bayrakları ve CORS Politikası",
            Description = "Canlı Set-Cookie yanıtlarında Secure, HttpOnly, SameSite bayraklarının ve Access-Control-Allow-Origin/Credentials (CORS) kurallarının testi.",
            Cwes = new List<string> { "CWE-614", "CWE-1004", "CWE-1275", "CWE-942" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "info_disclosure",
            Title = "Hassas Dosya, Ortam (.env/.git) ve API Şeması İfşa Testi",
            Description = ".env, .git/HEAD, .git/config, Swagger/OpenAPI şemaları, çalışma zamanı durum uç noktaları ve HTML kaynak kodu yorum/hata izi ifşası kontrolü.",
            Cwes = new List<string> { "CWE-200", "CWE-538", "CWE-552", "CWE-615" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "robots_wellknown",
            Title = "Robots.txt, Sitemap.xml ve Security.txt Keşif Analizi",
            Description = "robots.txt içindeki Disallow/Allow direktiflerinde hassas dizin ifşası, sitemap.xml erişilebilirliği ve RFC 9116 security.txt doğrulaması.",
            Cwes = new List<string> { "CWE-200", "CWE-548" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "form_client_security",
            Title = "İstemci Tarafı Script (SRI), HTML Form ve Yönlendirme Güvenliği",
            Description = "Harici CDN scriptlerinde Subresource Integrity (SRI) bütünlük kontrolü, güvensiz form hedefleri ve açık yönlendirme (Open Redirect) yüzeyi testi.",
            Cwes = new List<string> { "CWE-353", "CWE-319", "CWE-601" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "recon_discovery",
            Title = "Sunucu, Teknoloji Yığını ve Mimari Parmak İzi (Fingerprinting)",
            Description = "Server, X-Powered-By, X-AspNet-Version başlıkları, CDN/WAF izleri, meta generator etiketleri ve canlı yanıt süresi üzerinden teknoloji tespiti.",
            Cwes = new List<string> { "CWE-200", "CWE-497" },
            EnabledByDefault = true
        },
        new ScanCategoryDto
        {
            Id = "ai_config_audit",
            Title = "Yapay Zekâ Destekli Derinlemesine Konfigürasyon ve Mimari Analiz",
            Description = "Canlı HTTP yanıtlarından toplanan (hassas verileri maskelenmiş) gerçek telemetri üzerinden yapay zeka destekli derinlemesine güvenlik değerlendirmesi.",
            Cwes = new List<string> { "CWE-16", "CWE-200", "CWE-693" },
            EnabledByDefault = true
        }
    };

    public SecurityScanService(
        ISecurityScanRepository scanRepository,
        ISecurityScanConsentRepository consentRepository,
        ISecurityScanFindingRepository findingRepository,
        ISsrfProtectionService ssrfProtectionService,
        ISecurityScanRunnerService runnerService,
        ISecurityScanPdfService pdfService,
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IQuotaService quotaService,
        IUnitOfWork unitOfWork)
    {
        _scanRepository = scanRepository;
        _consentRepository = consentRepository;
        _findingRepository = findingRepository;
        _ssrfProtectionService = ssrfProtectionService;
        _runnerService = runnerService;
        _pdfService = pdfService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
    }

    public List<ScanCategoryDto> GetSupportedCategories()
    {
        return SupportedCategories;
    }

    public async Task<ValidateTargetResponseDto> ValidateTargetAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        return await _ssrfProtectionService.ValidateTargetUrlAsync(targetUrl, cancellationToken);
    }

    public async Task<SecurityScanResponseDto> CreateAndStartScanAsync(
        Guid userId,
        CreateSecurityScanRequestDto request,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // 1. Legal and Authorization Consent Validation
        if (!request.HasOwnershipAuthorization)
        {
            throw new InvalidOperationException("Güvenlik testi başlatılamaz: Hedef sistem üzerinde yasal yetki ve izne sahip olduğunuzu onaylamanız zorunludur.");
        }

        if (!request.HasRiskAcknowledged)
        {
            throw new InvalidOperationException("Güvenlik testi başlatılamaz: Test kapsamını, olası riskleri ve bilgilendirme metnini kabul etmeniz zorunludur.");
        }

        // 2. SSRF Validation
        var validation = await _ssrfProtectionService.ValidateTargetUrlAsync(request.TargetUrl, cancellationToken);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedUrl))
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Geçersiz veya kısıtlanmış hedef URL.");
        }

        // 3. Concurrency Protection (Max 1 active running scan per user)
        var hasActiveScan = await _scanRepository.HasActiveRunningScanAsync(userId, cancellationToken);
        if (hasActiveScan)
        {
            throw new InvalidOperationException("Halihazırda devam eden aktif bir güvenlik taramanız bulunmaktadır. Lütfen mevcut taramanın tamamlanmasını bekleyin veya iptal edin.");
        }

        // 4. Validate Categories and ScanMode (Reject unimplemented/unsupported categories or modes before consuming quota)
        var validCategoryIds = SupportedCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> selectedCategories;
        if (request.SelectedCategories != null && request.SelectedCategories.Count > 0)
        {
            selectedCategories = new List<string>(request.SelectedCategories);
        }
        else
        {
            selectedCategories = SupportedCategories.Where(c => c.EnabledByDefault).Select(c => c.Id).ToList();
        }

        var invalidCategories = selectedCategories.Where(c => !validCategoryIds.Contains(c)).ToList();
        if (invalidCategories.Count > 0)
        {
            throw new ArgumentException($"Desteklenmeyen veya uygulanamayan tarama kategorisi seçildi: {string.Join(", ", invalidCategories)}.");
        }

        var validScanModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "quick", "standard", "deep" };
        if (string.IsNullOrWhiteSpace(request.ScanMode) || !validScanModes.Contains(request.ScanMode.Trim()))
        {
            throw new ArgumentException($"Desteklenmeyen veya geçersiz tarama modu seçildi: '{request.ScanMode}'. Yalnızca 'quick', 'standard' ve 'deep' pasif tarama modları desteklenmektedir.");
        }
        var scanMode = request.ScanMode.Trim().ToLowerInvariant();

        // 4b. Server-Side Quota Consumption (DEV-BILL-010)
        var quotaResult = await _quotaService.TryConsumeCreditAsync(
            userId,
            provider: "security_scan",
            deviceId: null,
            ipAddress: clientIp,
            cancellationToken: cancellationToken
        );

        if (!quotaResult.Allowed)
        {
            throw new InvalidOperationException(quotaResult.RejectionReason ?? "Yetersiz kredi: Güvenlik taraması başlatabilmek için yeterli krediniz bulunmamaktadır.");
        }

        var creditDebited = quotaResult.Allowed && !quotaResult.IsByokBypass;
        int refundExecuted = 0;

        async Task CompensateCreditIfFailedAsync()
        {
            if (creditDebited && Interlocked.Exchange(ref refundExecuted, 1) == 0)
            {
                try
                {
                    await _quotaService.RefundCreditAsync(userId, CancellationToken.None);
                }
                catch
                {
                    // Best-effort compensation; ignore exception to preserve original error
                }
            }
        }

        try
        {
            // 5. Create SecurityScan Entity
            var scan = new SecurityScan
            {
                UserId = userId,
                ConversationId = request.ConversationId,
                TargetUrl = request.TargetUrl.Trim(),
                NormalizedTargetUrl = validation.NormalizedUrl,
                ScanMode = scanMode,
                SelectedCategories = string.Join(",", selectedCategories),
                CustomInstructions = request.CustomInstructions?.Trim(),
                Status = "Pending"
            };

            await _scanRepository.AddAsync(scan, cancellationToken);

            // 6. Create SecurityScanConsent Entity
            var consent = new SecurityScanConsent
            {
                SecurityScanId = scan.Id,
                UserId = userId,
                TargetUrl = validation.NormalizedUrl,
                DisclaimerVersion = request.DisclaimerVersion ?? "v1.0",
                HasOwnershipAuthorization = request.HasOwnershipAuthorization,
                HasRiskAcknowledged = request.HasRiskAcknowledged,
                ClientIp = clientIp,
                UserAgent = userAgent,
                ConsentedAt = DateTime.UtcNow
            };

            await _consentRepository.AddAsync(consent, cancellationToken);

            // 6b. Persist Conversation Messages for permanent chat history & RAG context
            Conversation? conversation = null;
            if (request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
            {
                conversation = await _conversationRepository.FirstOrDefaultAsync(
                    c => c.Id == request.ConversationId.Value && c.UserId == userId && !c.IsDeleted,
                    cancellationToken);
            }

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    Title = $"Pasif Güvenlik Taraması: {scan.TargetUrl}",
                    CreatedAt = DateTime.UtcNow
                };
                await _conversationRepository.AddAsync(conversation, cancellationToken);
            }

            scan.ConversationId = conversation.Id;

            var formattedScope = SecurityScanRunnerService.FormatCategoryLabels(scan.SelectedCategories);
            var promptText = !string.IsNullOrWhiteSpace(request.CustomInstructions)
                ? $"{request.TargetUrl} adresindeki web uygulamasının kamuya açık dış yüzeyini güvenlik ve yapılandırma açısından analiz et. (Kapsam: {formattedScope} | Mod: {scanMode.ToUpperInvariant()}). Ek Talimat: {request.CustomInstructions.Trim()}"
                : $"{request.TargetUrl} adresindeki web uygulamasının kamuya açık dış yüzeyini güvenlik ve yapılandırma açısından analiz et. (Kapsam: {formattedScope} | Mod: {scanMode.ToUpperInvariant()})";

            var userMessage = new Message
            {
                ConversationId = conversation.Id,
                UserId = userId,
                Role = "user",
                InputType = "text",
                Content = promptText,
                CreatedAt = DateTime.UtcNow
            };
            await _messageRepository.AddAsync(userMessage, cancellationToken);

            var modeDescription = scanMode switch
            {
                "quick" => "Quick (Yalnızca ana sayfa GET / başlıkları, SSL/TLS, çerezler, CORS ve parmak izi analizi)",
                "standard" => "Standard (Ana sayfa + kamuya açık keşif dosyaları: /robots.txt, /.well-known/security.txt, /sitemap.xml)",
                "deep" => "Deep (Ana sayfa + kamuya açık keşif + sınırlı hassas dosya ve şema kontrolleri: .env, .git, swagger/openapi)",
                _ => scanMode.ToUpperInvariant()
            };

            var assistantContent = $"🛡️ **{request.TargetUrl}** için güvenlik & konfigürasyon taraması başlatıldı.\n\n" +
              $"• **Hedef:** {validation.NormalizedUrl}\n" +
              $"• **Kapsam:** {formattedScope}\n" +
              $"• **Test Modu:** {modeDescription}\n" +
              $"• **Hukuki Yetki:** Onaylandı (Sürüm: {consent.DisclaimerVersion})\n" +
              $"• **Metodoloji:** Saldırgan olmayan salt-okunur HTTP/TLS ve konfigürasyon kontrolleri (SQLi, brute-force veya aktif exploit içermez)\n\n" +
              $"Güvenlik değerlendirmesi ve yapılandırma analizi aşağıda canlı olarak işlenmektedir.";

            var assistantMessage = new Message
            {
                ConversationId = conversation.Id,
                UserId = userId,
                Role = "assistant",
                InputType = "text",
                Content = assistantContent,
                CreatedAt = DateTime.UtcNow.AddMilliseconds(50)
            };
            await _messageRepository.AddAsync(assistantMessage, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            _conversationRepository.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Launch Background Runner
            await _runnerService.RunScanInBackgroundAsync(scan.Id, CancellationToken.None);

            return MapToResponseDto(scan, consent, new List<SecurityScanFinding>());
        }
        catch (Exception ex)
        {
            await CompensateCreditIfFailedAsync();

            if (IsActiveScanConcurrencyConflict(ex))
            {
                throw new InvalidOperationException(
                    "Halihazırda devam eden aktif bir güvenlik taramanız bulunmaktadır. Lütfen mevcut taramanın tamamlanmasını bekleyin veya iptal edin.",
                    ex);
            }

            throw;
        }
    }

    public static bool IsActiveScanConcurrencyConflict(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            var typeName = current.GetType().Name;
            var message = current.Message ?? string.Empty;
            if (message.Contains("uq_security_scans_active_per_user", StringComparison.OrdinalIgnoreCase)
                || message.Contains("security_scans.user_id", StringComparison.OrdinalIgnoreCase)
                || message.Contains("23505", StringComparison.OrdinalIgnoreCase)
                || (typeName.Contains("DbUpdateException", StringComparison.OrdinalIgnoreCase)
                    && (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }

    public async Task<SecurityScanResponseDto?> GetScanByIdAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        var scan = await _scanRepository.GetScanWithDetailsAsync(scanId, userId, cancellationToken);
        if (scan == null)
        {
            return null;
        }

        return MapToResponseDto(scan, scan.Consent, scan.Findings.ToList());
    }

    public async Task<List<SecurityScanSummaryDto>> GetUserScansAsync(Guid userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var scans = await _scanRepository.GetUserScansAsync(userId, page, pageSize, cancellationToken);
        return scans.Select(s => new SecurityScanSummaryDto
        {
            Id = s.Id,
            TargetUrl = s.TargetUrl,
            ScanMode = s.ScanMode,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            CompletedAt = s.CompletedAt,
            TotalFindingsCount = s.TotalFindingsCount,
            CriticalCount = s.CriticalCount,
            HighCount = s.HighCount,
            MediumCount = s.MediumCount,
            LowCount = s.LowCount,
            InfoCount = s.InfoCount
        }).ToList();
    }

    public async Task<bool> CancelScanAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        var scan = await _scanRepository.FirstOrDefaultAsync(s => s.Id == scanId && s.UserId == userId && !s.IsDeleted, cancellationToken);
        if (scan == null)
        {
            return false;
        }

        if (scan.Status != "Running" && scan.Status != "Pending")
        {
            return false;
        }

        var cancelled = _runnerService.TryCancelScan(scanId);
        scan.Status = "Cancelled";
        scan.ErrorMessage = "Tarama kullanıcı tarafından iptal edildi.";
        scan.CompletedAt = DateTime.UtcNow;
        _scanRepository.Update(scan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<byte[]> GenerateScanPdfReportAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        var scan = await _scanRepository.GetScanWithDetailsAsync(scanId, userId, cancellationToken);
        if (scan == null)
        {
            throw new KeyNotFoundException("Güvenlik taraması bulunamadı veya erişim yetkiniz yok.");
        }

        return _pdfService.GenerateScanReportPdf(scan, scan.Findings.ToList(), scan.Consent);
    }

    public async Task<byte[]> GenerateConsentPdfReportAsync(Guid scanId, Guid userId, CancellationToken cancellationToken = default)
    {
        var scan = await _scanRepository.GetScanWithDetailsAsync(scanId, userId, cancellationToken);
        if (scan == null || scan.Consent == null)
        {
            throw new KeyNotFoundException("Onay kaydı veya tarama bulunamadı.");
        }

        return _pdfService.GenerateConsentReportPdf(scan, scan.Consent);
    }

    private static SecurityScanResponseDto MapToResponseDto(SecurityScan scan, SecurityScanConsent? consent, List<SecurityScanFinding> findings)
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
            Consent = consent != null ? new SecurityScanConsentDto
            {
                Id = consent.Id,
                SecurityScanId = consent.SecurityScanId,
                TargetUrl = consent.TargetUrl,
                DisclaimerVersion = consent.DisclaimerVersion,
                HasOwnershipAuthorization = consent.HasOwnershipAuthorization,
                HasRiskAcknowledged = consent.HasRiskAcknowledged,
                ConsentedAt = consent.ConsentedAt,
                ClientIp = consent.ClientIp
            } : null,
            Findings = findings.Select(f => new SecurityScanFindingDto
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
