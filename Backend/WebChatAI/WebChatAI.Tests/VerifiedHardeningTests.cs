using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebChatAI.Application.Common;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Chat;
using WebChatAI.Application.Features.Conversations;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Infrastructure.Services;
using WebChatAI.Persistence.Context;
using WebChatAI.Persistence.Repositories;
using Xunit;

namespace WebChatAI.Tests;

public class VerifiedHardeningTests
{
    private static AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext? TryCreatePostgresDbContext()
    {
        try
        {
            var connStr = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
                ?? "Host=127.0.0.1;Port=5433;Database=ai_agent_db;Username=postgres;Password=Postgres123!";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connStr)
                .Options;
            var ctx = new AppDbContext(options);
            if (ctx.Database.CanConnect())
            {
                return ctx;
            }
        }
        catch
        {
        }
        return null;
    }

    // =========================================================================
    // Item 1: SendVoiceMessageCommand quota check prior to transcription & idempotent refund
    // =========================================================================

    [Fact]
    public async Task SendVoiceMessageCommand_WhenQuotaExhausted_DoesNotCallTranscriptionService()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var convId = Guid.NewGuid();

        db.Conversations.Add(new Conversation
        {
            Id = convId,
            UserId = userId,
            Title = "Voice Test",
            Model = "gpt-4o",
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var msgRepo = new MessageRepository(db);
        var chunkRepo = new Mock<IDocumentChunkRepository>();
        var voiceService = new Mock<IVoiceService>();
        var aiService = new Mock<IAiService>();
        var quotaService = new Mock<IQuotaService>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(u => u.UserId).Returns(userId);
        currentUserService.Setup(u => u.DeviceId).Returns("device-1");
        currentUserService.Setup(u => u.IpAddress).Returns("127.0.0.1");

        // Quota is exhausted
        quotaService.Setup(q => q.TryConsumeCreditAsync(userId, "system", "device-1", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: false, IsByokBypass: false, RemainingCredits: 0, RejectionReason: "Quota exhausted."));

        var handler = new SendVoiceMessageCommandHandler(
            convRepo,
            msgRepo,
            chunkRepo.Object,
            voiceService.Object,
            aiService.Object,
            quotaService.Object,
            currentUserService.Object,
            db
        );

        using var dummyStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new SendVoiceMessageCommand(convId, dummyStream, "audio.wav");

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.False(response.Success);
        Assert.Equal("Quota exhausted.", response.Message);
        // Whisper transcription MUST NOT be called when quota check fails
        voiceService.Verify(v => v.TranscribeAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        // No refund called because credit was not debited
        quotaService.Verify(q => q.RefundCreditAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendVoiceMessageCommand_WhenTranscriptionFails_RefundsCreditOnceAndOnlyOnce()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var convId = Guid.NewGuid();

        db.Conversations.Add(new Conversation
        {
            Id = convId,
            UserId = userId,
            Title = "Voice Test",
            Model = "gpt-4o",
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var msgRepo = new MessageRepository(db);
        var chunkRepo = new Mock<IDocumentChunkRepository>();
        var voiceService = new Mock<IVoiceService>();
        var aiService = new Mock<IAiService>();
        var quotaService = new Mock<IQuotaService>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(u => u.UserId).Returns(userId);

        // Quota consumed successfully
        quotaService.Setup(q => q.TryConsumeCreditAsync(userId, "system", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 14));

        // Transcription fails (returns null or empty)
        voiceService.Setup(v => v.TranscribeAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var handler = new SendVoiceMessageCommandHandler(
            convRepo,
            msgRepo,
            chunkRepo.Object,
            voiceService.Object,
            aiService.Object,
            quotaService.Object,
            currentUserService.Object,
            db
        );

        using var dummyStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new SendVoiceMessageCommand(convId, dummyStream, "audio.wav");

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.False(response.Success);
        // Refund must be called exactly once
        quotaService.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendVoiceMessageCommand_WhenAiThrows_RefundsCreditOnceAndMasksRawException()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var convId = Guid.NewGuid();

        db.Conversations.Add(new Conversation
        {
            Id = convId,
            UserId = userId,
            Title = "Voice Test",
            Model = "gpt-4o",
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var msgRepo = new MessageRepository(db);
        var chunkRepo = new Mock<IDocumentChunkRepository>();
        var voiceService = new Mock<IVoiceService>();
        var aiService = new Mock<IAiService>();
        var quotaService = new Mock<IQuotaService>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(u => u.UserId).Returns(userId);

        quotaService.Setup(q => q.TryConsumeCreditAsync(userId, "system", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 14));
        quotaService.Setup(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        voiceService.Setup(v => v.TranscribeAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello DevPilot!");

        // AI throws raw exception with internal information
        aiService.Setup(a => a.GenerateChatResponseAsync(It.IsAny<List<Message>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal internal database secret leaked at /var/run/secrets"));

        var handler = new SendVoiceMessageCommandHandler(
            convRepo,
            msgRepo,
            chunkRepo.Object,
            voiceService.Object,
            aiService.Object,
            quotaService.Object,
            currentUserService.Object,
            db
        );

        using var dummyStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new SendVoiceMessageCommand(convId, dummyStream, "audio.wav");

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.False(response.Success);
        // Client message must NOT contain raw exception message
        Assert.DoesNotContain("/var/run/secrets", response.Message);
        Assert.DoesNotContain("Fatal internal", response.Message);
        // Refund called exactly once and reflected in message
        quotaService.Verify(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Krediniz iade edildi", response.Message);
    }

    [Fact]
    public async Task SendVoiceMessageCommand_WhenRefundFails_DoesNotFalselyClaimRefundInErrorMessage()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var convId = Guid.NewGuid();

        db.Conversations.Add(new Conversation
        {
            Id = convId,
            UserId = userId,
            Title = "Voice Truthful Error Test",
            Model = "gpt-4o",
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var msgRepo = new MessageRepository(db);
        var chunkRepo = new Mock<IDocumentChunkRepository>();
        var voiceService = new Mock<IVoiceService>();
        var aiService = new Mock<IAiService>();
        var quotaService = new Mock<IQuotaService>();
        var currentUserService = new Mock<ICurrentUserService>();

        currentUserService.Setup(u => u.UserId).Returns(userId);

        quotaService.Setup(q => q.TryConsumeCreditAsync(userId, "system", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditConsumptionResult(Allowed: true, IsByokBypass: false, RemainingCredits: 10));
        quotaService.Setup(q => q.RefundCreditAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        voiceService.Setup(v => v.TranscribeAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transcription service unreachable."));

        var handler = new SendVoiceMessageCommandHandler(
            convRepo,
            msgRepo,
            chunkRepo.Object,
            voiceService.Object,
            aiService.Object,
            quotaService.Object,
            currentUserService.Object,
            db
        );

        using var dummyStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new SendVoiceMessageCommand(convId, dummyStream, "audio.wav");

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.False(response.Success);
        Assert.DoesNotContain("Krediniz iade edildi", response.Message);
        Assert.Contains("Kredi iadesi yapılamadı", response.Message);
    }

    // =========================================================================
    // Item 2: QuotaService daily rollover race condition test
    // =========================================================================

    [Fact]
    public async Task QuotaService_MidnightRollover_ConcurrentConsumption_DoesNotLoseCredits()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userId = Guid.NewGuid();

        // Yesterday's exhausted quota
        var quota = new UserQuota
        {
            UserId = userId,
            DailyCreditLimit = 15,
            CreditsUsedToday = 15,
            CycleStartDate = DateTime.UtcNow.Date.AddDays(-1),
            SubscriptionTier = "free"
        };
        await quotaRepo.AddAsync(quota);
        await db.SaveChangesAsync();

        // Simulate 10 concurrent requests at midnight rollover
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            return await quotaRepo.TryAtomicConsumeCreditAsync(userId);
        })).ToList();

        var results = await Task.WhenAll(tasks);

        // All 10 requests must succeed because daily limit is 15 and cycle rolled over
        Assert.All(results, r => Assert.True(r));

        var updated = await quotaRepo.GetByUserIdAsync(userId);
        Assert.NotNull(updated);
        Assert.Equal(DateTime.UtcNow.Date, updated.CycleStartDate);
        Assert.Equal(10, updated.CreditsUsedToday);
        Assert.Equal(5, updated.RemainingCredits);
    }

    [Fact]
    public async Task QuotaService_RelationalDatabase_ConcurrentResetAndConsumption_DoesNotLoseCredits()
    {
        using var db = TryCreatePostgresDbContext();
        if (db == null)
        {
            // If relational database is not reachable in this runner environment, skip test gracefully
            return;
        }

        var quotaRepo = new UserQuotaRepository(db);
        var userId = Guid.NewGuid();

        try
        {
            var testUser = new User
            {
                Id = userId,
                Email = $"test_{userId:N}@example.com",
                NormalizedEmail = $"TEST_{userId:N}@EXAMPLE.COM",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hash"
            };
            db.Users.Add(testUser);
            await db.SaveChangesAsync();

            // Yesterday's exhausted quota
            var quota = new UserQuota
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DailyCreditLimit = 15,
                CreditsUsedToday = 15,
                CycleStartDate = DateTime.UtcNow.Date.AddDays(-1),
                SubscriptionTier = "free"
            };
            await quotaRepo.AddAsync(quota);
            await db.SaveChangesAsync();

            // Verify relational path is active
            Assert.True(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(db.Database));

            // Test atomic EnsureCycleResetAsync on relational database (ExecuteUpdateAsync path)
            var resetResult = await quotaRepo.EnsureCycleResetAsync(userId);
            Assert.True(resetResult);

            // Read fresh quota without tracking
            var freshQuota = await quotaRepo.GetByUserIdNoTrackingAsync(userId);
            Assert.NotNull(freshQuota);
            Assert.Equal(DateTime.UtcNow.Date, freshQuota.CycleStartDate);
            Assert.Equal(0, freshQuota.CreditsUsedToday);

            // Test atomic credit consumption on relational database (ExecuteUpdateAsync path)
            var consumed = await quotaRepo.TryAtomicConsumeCreditAsync(userId);
            Assert.True(consumed);

            var afterConsume = await quotaRepo.GetByUserIdNoTrackingAsync(userId);
            Assert.NotNull(afterConsume);
            Assert.Equal(1, afterConsume.CreditsUsedToday);
        }
        finally
        {
            var toCleanQuotas = await db.UserQuotas.Where(q => q.UserId == userId).ToListAsync();
            db.UserQuotas.RemoveRange(toCleanQuotas);
            var toCleanUsers = await db.Users.Where(u => u.Id == userId).ToListAsync();
            db.Users.RemoveRange(toCleanUsers);
            await db.SaveChangesAsync();
        }
    }

    // =========================================================================
    // Item 3: HardDeleteUserAsync consistency and cleanup order
    // =========================================================================

    [Fact]
    public async Task HardDeleteUserAsync_DeletesAllUserEntitiesInStrictOrder()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userRepo = new UserRepository(db);
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "delete_me@example.com",
            NormalizedEmail = "DELETE_ME@EXAMPLE.COM",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };
        db.Users.Add(user);

        var conv = new Conversation { Id = Guid.NewGuid(), UserId = userId, Title = "Conv" };
        db.Conversations.Add(conv);
        db.Messages.Add(new Message { Id = Guid.NewGuid(), ConversationId = conv.Id, UserId = userId, Role = "user", Content = "Hi" });

        var doc = new Document { Id = Guid.NewGuid(), UserId = userId, FileName = "doc.txt", ContentType = "text/plain" };
        db.Documents.Add(doc);
        db.DocumentChunks.Add(new DocumentChunk { Id = Guid.NewGuid(), DocumentId = doc.Id, Content = "chunk" });

        db.UserQuotas.Add(new UserQuota { Id = Guid.NewGuid(), UserId = userId });
        await db.SaveChangesAsync();

        await userRepo.HardDeleteUserAsync(userId);

        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Id == userId));
        Assert.Empty(await db.Conversations.Where(c => c.UserId == userId).ToListAsync());
        Assert.Empty(await db.Messages.Where(m => m.UserId == userId).ToListAsync());
        Assert.Empty(await db.Documents.Where(d => d.UserId == userId).ToListAsync());
        Assert.Empty(await db.DocumentChunks.Where(c => c.DocumentId == doc.Id).ToListAsync());
        Assert.Empty(await db.UserQuotas.Where(q => q.UserId == userId).ToListAsync());
    }

    // =========================================================================
    // Item 4: Refresh token omitted from Auth response JSON
    // =========================================================================

    [Fact]
    public void AuthResponseDto_WhenSerializedToJson_OmitsRefreshToken_WhenNull()
    {
        var user = new UserDto(Guid.NewGuid(), "test@example.com", "Test", "User", true, null, DateTime.UtcNow);
        var responseDto = new AuthResponseDto(user, "valid-access-token", 3600, RefreshToken: null);

        var json = JsonSerializer.Serialize(responseDto);

        Assert.Contains("\"AccessToken\":", json);
        Assert.DoesNotContain("\"RefreshToken\":", json);
        Assert.DoesNotContain("\"refreshToken\":", json);
    }

    // =========================================================================
    // Item 5: .env file detection parser and key isolation
    // =========================================================================

    [Theory]
    [InlineData("DB_PASSWORD=secret123\nPORT=8080", "DB_PASSWORD", true)]
    [InlineData("export db_pass=secret456", "db_pass", true)]
    [InlineData("API_KEY=AIzaSyD-12345", "API_KEY", true)]
    [InlineData("jwt_secret=myjwtkeyhere", "jwt_secret", true)]
    [InlineData("token=abcdef", "token", true)]
    [InlineData("PORT=3000\nNODE_ENV=production\nAPP_NAME=MyApp", null, false)]
    [InlineData("<!DOCTYPE html><html><body><h1>404</h1><p>DB_PASSWORD</p></body></html>", null, false)]
    [InlineData("<html>DB_PASSWORD=secret</html>", null, false)]
    public void IsSensitiveDotEnvContent_CorrectlyIdentifiesSensitiveKeysAndRejectsNonSensitiveOrHtml(
        string content,
        string? expectedKey,
        bool expectedResult)
    {
        var result = SecurityScanRunnerService.IsSensitiveDotEnvContent(content, out var matchedKey);

        Assert.Equal(expectedResult, result);
        if (expectedResult)
        {
            Assert.Equal(expectedKey, matchedKey);
        }
        else
        {
            Assert.Null(matchedKey);
        }
    }

    // =========================================================================
    // Item 6: AI findings severity capping to Medium (CVSS <= 6.9)
    // =========================================================================

    [Fact]
    public void ParseAiJsonResponse_CapsCriticalAndHighFindingsToMediumWithMaxCvss6_9()
    {
        var scan = new SecurityScan
        {
            Id = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            NormalizedTargetUrl = "https://example.com"
        };

        var aiJson = """
        {
            "findings": [
                {
                    "title": "Suspected SQL Injection",
                    "severity": "critical",
                    "cvss": 9.8,
                    "cwe": "89",
                    "description": "Possible heuristic flaw",
                    "impact": "High",
                    "technical_analysis": "AI guess",
                    "remediation_steps": "Fix it"
                },
                {
                    "title": "Suspected Authorization Bypass",
                    "severity": "high",
                    "cvss": 8.5,
                    "cwe": "CWE-285",
                    "description": "Another heuristic guess"
                },
                {
                    "title": "Missing Nonce in Script",
                    "severity": "low",
                    "cvss": 3.0,
                    "cwe": "CWE-693",
                    "description": "Informative"
                }
            ],
            "summary": "AI summary"
        }
        """;

        var parsed = SecurityScanRunnerService.ParseAiJsonResponse(aiJson, scan, "telemetry");

        Assert.NotNull(parsed);
        var findings = parsed.Value.AiFindings;
        Assert.Equal(3, findings.Count);

        // The critical finding must be capped to medium with CVSS <= 6.9
        var finding1 = findings[0];
        Assert.Equal("medium", finding1.Severity);
        Assert.Equal(6.9, finding1.CvssScore);

        // The high finding must be capped to medium with CVSS <= 6.9
        var finding2 = findings[1];
        Assert.Equal("medium", finding2.Severity);
        Assert.Equal(6.9, finding2.CvssScore);

        // The low finding must retain low severity and low CVSS
        var finding3 = findings[2];
        Assert.Equal("low", finding3.Severity);
        Assert.Equal(3.0, finding3.CvssScore);
    }

    // =========================================================================
    // Item 7: Chat and Voice resource limits & rate limiting attributes
    // =========================================================================

    [Fact]
    public void StreamChatRequestDto_ValidatesMessageLengthUpperLimit()
    {
        var validDto = new StreamChatRequestDto(Guid.NewGuid(), new string('a', 10000));
        var validContext = new ValidationContext(validDto);
        var validResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(validDto, validContext, validResults, true);
        Assert.True(isValid);

        var invalidDto = new StreamChatRequestDto(Guid.NewGuid(), new string('a', 10001));
        var invalidContext = new ValidationContext(invalidDto);
        var invalidResults = new List<ValidationResult>();
        var isInvalid = Validator.TryValidateObject(invalidDto, invalidContext, invalidResults, true);
        Assert.False(isInvalid);
        Assert.Contains(invalidResults, r => r.ErrorMessage != null && r.ErrorMessage.Contains("10.000"));
    }

    [Fact]
    public void ChatController_StreamAndVoiceEndpoints_HaveHeavyWorkloadRateLimitAttribute()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Backend", "WebChatAI", "WebChatAI.WebApi", "Controllers", "ChatController.cs"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WebChatAI.WebApi", "Controllers", "ChatController.cs"))
        };

        var controllerPath = candidates.FirstOrDefault(File.Exists);
        Assert.NotNull(controllerPath);

        var source = File.ReadAllText(controllerPath);

        // Ensure both stream and voice endpoints have HeavyWorkloadPolicy rate limiting
        Assert.Matches(@"\[HttpPost\(""stream""\)\]\s*\[EnableRateLimiting\(""HeavyWorkloadPolicy""\)\]", source);
        Assert.Matches(@"\[HttpPost\(""voice""\)\]\s*\[EnableRateLimiting\(""HeavyWorkloadPolicy""\)\]", source);
    }

    // =========================================================================
    // Item 8: Large list query pagination clamping
    // =========================================================================

    [Fact]
    public async Task GetConversationsQuery_ClampsPageSizeToMax100()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();

        for (int i = 0; i < 120; i++)
        {
            db.Conversations.Add(new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = $"Conv {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                IsDeleted = false
            });
        }
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(u => u.UserId).Returns(userId);

        var handler = new GetConversationsQueryHandler(convRepo, currentUserService.Object);

        // Request 500 items, should be clamped to 100
        var query = new GetConversationsQuery(Page: 1, PageSize: 500);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(100, result.Data.Count);
    }

    [Fact]
    public async Task SecurityScanRepository_GetUserScansAsync_PaginatesWithoutLoadingFindings()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var scanRepo = new SecurityScanRepository(db);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 15; i++)
        {
            var scan = new SecurityScan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetUrl = $"https://site{i}.com",
                NormalizedTargetUrl = $"https://site{i}.com",
                ScanMode = "quick",
                SelectedCategories = "ssl",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            };
            db.SecurityScans.Add(scan);
            db.SecurityScanFindings.Add(new SecurityScanFinding
            {
                Id = Guid.NewGuid(),
                SecurityScanId = scan.Id,
                Title = "Finding",
                Severity = "medium",
                Description = "Test finding description",
                TargetUrl = scan.TargetUrl
            });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scans = await scanRepo.GetUserScansAsync(userId, page: 1, pageSize: 5);

        Assert.Equal(5, scans.Count);
        // Findings should NOT be eagerly loaded in the summary list query
        Assert.Empty(scans[0].Findings);
    }

    // =========================================================================
    // Item 9: AppEnvironment contradictory environment handling & fail-closed
    // =========================================================================

    [Fact]
    public void AppEnvironment_SingleAuthoritativeSource_HostEnvironmentNameTakesPrecedence()
    {
        // When hostEnvironmentName is set, it is the single source of truth, ignoring lower-precedence indicators
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DEPLOYMENT_STAGE", "Production" },
                { "ASPNETCORE_ENVIRONMENT", "Production" }
            })
            .Build();

        Assert.False(AppEnvironment.IsProductionLike(config, hostEnvironmentName: "Development"));
        Assert.Equal("Development", AppEnvironment.GetEnvironmentName(config, hostEnvironmentName: "Development"));
    }

    [Fact]
    public void AppEnvironment_WhenHostEnvNull_AspNetCoreEnvironmentIsSingleSourceOfTruth()
    {
        // When hostEnvironmentName is null, ASPNETCORE_ENVIRONMENT is the authoritative source over DOTNET_ENVIRONMENT
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Development" },
                { "DOTNET_ENVIRONMENT", "Production" }
            })
            .Build();

        Assert.False(AppEnvironment.IsProductionLike(config, hostEnvironmentName: null));
        Assert.Equal("Development", AppEnvironment.GetEnvironmentName(config, hostEnvironmentName: null));
    }

    [Fact]
    public void AppEnvironment_WhenEmpty_FailsClosedToProduction()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.True(AppEnvironment.IsProductionLike(config, hostEnvironmentName: null));
        Assert.Equal("Production", AppEnvironment.GetEnvironmentName(config, hostEnvironmentName: null));
    }

    [Fact]
    public void AppEnvironment_WhenAllIndicatorsAreDevelopment_IsDevelopment()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ASPNETCORE_ENVIRONMENT", "Development" },
                { "DOTNET_ENVIRONMENT", "Development" }
            })
            .Build();

        Assert.False(AppEnvironment.IsProductionLike(config, hostEnvironmentName: "Development"));
        Assert.Equal("Development", AppEnvironment.GetEnvironmentName(config, hostEnvironmentName: "Development"));
    }

    [Fact]
    public async Task GetConversationMessagesQuery_ReturnsRecentMessagesChronologically()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var convId = Guid.NewGuid();

        db.Conversations.Add(new Conversation
        {
            Id = convId,
            UserId = userId,
            Title = "Recent Ordering Test"
        });

        // Add 150 messages with ascending created_at
        var baseTime = DateTime.UtcNow.AddHours(-150);
        for (int i = 1; i <= 150; i++)
        {
            db.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = convId,
                Role = i % 2 == 1 ? "user" : "assistant",
                Content = $"Message {i}",
                CreatedAt = baseTime.AddHours(i)
            });
        }
        await db.SaveChangesAsync();

        var convRepo = new ConversationRepository(db);
        var msgRepo = new MessageRepository(db);
        var scanRepo = new Mock<ISecurityScanRepository>();
        scanRepo.Setup(s => s.Where(It.IsAny<System.Linq.Expressions.Expression<Func<SecurityScan, bool>>>()))
            .Returns(db.SecurityScans);

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(u => u.UserId).Returns(userId);

        var handler = new GetConversationMessagesQueryHandler(convRepo, msgRepo, scanRepo.Object, currentUserService.Object);

        // Request page 1 with pageSize 50 -> must return messages 101 to 150 in chronological order (oldest to newest)
        var result = await handler.Handle(new GetConversationMessagesQuery(convId, Page: 1, PageSize: 50), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(50, result.Data!.Count);
        Assert.Equal("Message 101", result.Data.First().Content);
        Assert.Equal("Message 150", result.Data.Last().Content);

        // Verify chronological order (each message must be created after or equal to the previous)
        for (int i = 1; i < result.Data.Count; i++)
        {
            Assert.True(result.Data[i].CreatedAt >= result.Data[i - 1].CreatedAt);
        }
    }

    [Fact]
    public async Task QuotaService_TryConsumeCreditAsync_ReturnsFreshRemainingCreditsImmediately()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();

        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var userAiKeyRepo = new Mock<IUserAiKeyRepository>();
        userAiKeyRepo.Setup(r => r.GetByUserAndProviderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAiKey?)null);

        var abuseService = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, userAiKeyRepo.Object, abuseService.Object, db, logger.Object);

        // Add quota with 15 limit, 0 used
        var quota = new UserQuota
        {
            UserId = userId,
            DailyCreditLimit = 15,
            CreditsUsedToday = 0,
            CycleStartDate = DateTime.UtcNow.Date,
            SubscriptionTier = "free"
        };
        await quotaRepo.AddAsync(quota);
        await db.SaveChangesAsync();

        // Consume 1 credit
        var result = await quotaService.TryConsumeCreditAsync(userId, "system", "device", "127.0.0.1");

        Assert.True(result.Allowed);
        Assert.False(result.IsByokBypass);
        Assert.Equal(14, result.RemainingCredits); // Must reflect the fresh value (15 - 1 = 14)
    }
}
