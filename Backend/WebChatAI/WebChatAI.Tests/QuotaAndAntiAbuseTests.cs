using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebChatAI.Application.DTOs;
using WebChatAI.Application.Features.Auth;
using WebChatAI.Application.Interfaces;
using WebChatAI.Domain.Entities;
using WebChatAI.Infrastructure.Services;
using WebChatAI.Persistence.Context;
using WebChatAI.Persistence.Repositories;
using Xunit;

namespace WebChatAI.Tests;

public class QuotaAndAntiAbuseTests
{
    private static AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Security:DeviceSalt", "Test_AntiAbuse_Salt_12345"}
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task Scenario1_NormalUser_ConsumingCredits1To15_DecrementsCorrectly()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "device_hash_1", CancellationToken.None);

        for (int i = 1; i <= 15; i++)
        {
            var result = await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
            Assert.True(result.Allowed, $"Request {i} should be allowed");
            Assert.False(result.IsByokBypass);
            Assert.Equal(15 - i, result.RemainingCredits);
        }

        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(0, status.RemainingCredits);
        Assert.Equal(15, status.UsedCredits);
    }

    [Fact]
    public async Task Scenario2_Request16_RejectedWithQuotaExhausted()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "device_hash_1", CancellationToken.None);

        // Consume all 15 credits
        for (int i = 0; i < 15; i++)
        {
            await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        }

        // 16th request must be rejected
        var result16 = await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        Assert.False(result16.Allowed);
        Assert.Equal(0, result16.RemainingCredits);
        Assert.NotNull(result16.RejectionReason);
        Assert.NotNull(result16.ResetAt);
    }

    [Fact]
    public async Task Scenario3_ConcurrentRequests_RaceConditionPrevented_NeverExceeds15()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateInMemoryDbContext(dbName);
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "device_hash_1", CancellationToken.None);

        // 25 concurrent requests launched in parallel (simulating separate scoped DI requests)
        var tasks = new List<Task<CreditConsumptionResult>>();
        for (int i = 0; i < 25; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var reqDb = CreateInMemoryDbContext(dbName);
                var qRepo = new UserQuotaRepository(reqDb);
                var uRepo = new UserRepository(reqDb);
                var kRepo = new UserAiKeyRepository(reqDb);
                var svc = new QuotaService(qRepo, uRepo, kRepo, abuseDetection.Object, reqDb, logger.Object);
                return await svc.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
            }));
        }

        var results = await Task.WhenAll(tasks);

        var allowedCount = results.Count(r => r.Allowed);
        var rejectedCount = results.Count(r => !r.Allowed);

        Assert.Equal(15, allowedCount);
        Assert.Equal(10, rejectedCount);

        db.ChangeTracker.Clear();
        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(15, status.UsedCredits);
        Assert.Equal(0, status.RemainingCredits);
    }

    [Fact]
    public async Task Scenario4_SoftDeletingMessages_DoesNotRestoreCredits()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "device_1", CancellationToken.None);

        // Consume 5 credits
        for (int i = 0; i < 5; i++)
        {
            await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        }

        // Simulate creating and soft-deleting messages in messages table
        var msg = new Message { ConversationId = Guid.NewGuid(), UserId = userId, Role = "user", Content = "test", IsDeleted = true };
        db.Messages.Add(msg);
        await db.SaveChangesAsync();

        // Credits must still reflect 5 used, 10 remaining (independent of messages table)
        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(5, status.UsedCredits);
        Assert.Equal(10, status.RemainingCredits);
    }

    [Fact]
    public async Task Scenario5_DeletingConversation_DoesNotRestoreCredits()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "device_1", CancellationToken.None);

        for (int i = 0; i < 10; i++)
        {
            await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        }

        // Delete conversation
        var conv = new Conversation { Id = Guid.NewGuid(), UserId = userId, Title = "Test", IsDeleted = true };
        db.Conversations.Add(conv);
        await db.SaveChangesAsync();

        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(10, status.UsedCredits);
        Assert.Equal(5, status.RemainingCredits);
    }

    [Fact]
    public async Task Scenario6_CycleReset_ResetsCreditsAfter24Hours()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        // Create quota with cycle start date 2 days ago
        var quota = new UserQuota
        {
            UserId = userId,
            DailyCreditLimit = 15,
            CreditsUsedToday = 15,
            CycleStartDate = DateTime.UtcNow.Date.AddDays(-2)
        };
        db.UserQuotas.Add(quota);
        await db.SaveChangesAsync();

        // Check quota status - should reset automatically
        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(0, status.UsedCredits);
        Assert.Equal(15, status.RemainingCredits);
    }

    [Fact]
    public async Task Scenario7_ByokRequests_BypassSystemQuota_EvenWhenCreditsAreZero()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        // User with 0 credits
        await quotaService.InitializeUserQuotaAsync(userId, 0, "device_1", CancellationToken.None);

        // Add active BYOK OpenAI key
        var byokKey = new UserAiKey
        {
            UserId = userId,
            Provider = "openai",
            EncryptedApiKey = "enc_key",
            KeyHint = "sk-...1234",
            IsActive = true,
            IsValid = true
        };
        db.UserAiKeys.Add(byokKey);
        await db.SaveChangesAsync();

        // System request should fail
        var systemResult = await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        Assert.False(systemResult.Allowed);

        // BYOK OpenAI request must succeed and NOT decrement credits
        var byokResult = await quotaService.TryConsumeCreditAsync(userId, "openai", "device_1", "127.0.0.1", CancellationToken.None);
        Assert.True(byokResult.Allowed);
        Assert.True(byokResult.IsByokBypass);
    }

    [Fact]
    public void Scenario8_DisposableEmailValidator_CorrectlyIdentifiesBurnerDomains()
    {
        var validator = new DisposableEmailValidator();

        Assert.True(validator.IsDisposableEmail("test@mailinator.com"));
        Assert.True(validator.IsDisposableEmail("user@tempmail.com"));
        Assert.True(validator.IsDisposableEmail("fake@10minutemail.com"));
        Assert.True(validator.IsDisposableEmail("bot@guerrillamail.com"));
        Assert.True(validator.IsDisposableEmail("burner@yopmail.com"));
        Assert.True(validator.IsDisposableEmail("sub@trashmail.com"));

        // Legitimate domains must pass
        Assert.False(validator.IsDisposableEmail("yasar@gmail.com"));
        Assert.False(validator.IsDisposableEmail("dev@company.com"));
        Assert.False(validator.IsDisposableEmail("student@university.edu.tr"));
        Assert.False(validator.IsDisposableEmail("admin@devpilot.ai"));
    }

    [Fact]
    public async Task Scenario9_SecondRegistrationOnSameDeviceWithin24h_AllocatesZeroCredits()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var deviceRepo = new DeviceRegistrationRepository(db);
        var auditRepo = new AbuseAuditLogRepository(db);
        var config = CreateMockConfiguration();
        var logger = new Mock<ILogger<AbuseDetectionService>>();

        var service = new AbuseDetectionService(deviceRepo, auditRepo, db, logger.Object, config);

        var rawDeviceId = "device_uuid_9988776655";

        // 1st Registration today: gets 15 credits
        var result1 = await service.EvaluateRegistrationRiskAsync(rawDeviceId, "192.168.1.50", "Mozilla/5.0", "user1@realmail.com");
        Assert.True(result1.IsAllowed);
        Assert.True(result1.ShouldAllocateFreeCredits);
        Assert.Equal(15, result1.InitialCreditLimit);

        // 2nd Registration from same device today: allowed to register, but receives 0 initial free credits
        var result2 = await service.EvaluateRegistrationRiskAsync(rawDeviceId, "192.168.1.50", "Mozilla/5.0", "user2@anothermail.com");
        Assert.True(result2.IsAllowed);
        Assert.False(result2.ShouldAllocateFreeCredits);
        Assert.Equal(0, result2.InitialCreditLimit);
        Assert.NotNull(result2.WarningMessage);
    }

    [Fact]
    public void Scenario10_DeviceFingerprintHashing_IsDeterministicAndIrreversible()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var deviceRepo = new DeviceRegistrationRepository(db);
        var auditRepo = new AbuseAuditLogRepository(db);
        var config = CreateMockConfiguration();
        var logger = new Mock<ILogger<AbuseDetectionService>>();

        var service = new AbuseDetectionService(deviceRepo, auditRepo, db, logger.Object, config);

        var hash1 = service.HashDeviceId("fixed_client_uuid_123", "Mozilla/5.0 Chrome", "88.240.10.15");
        var hash2 = service.HashDeviceId("fixed_client_uuid_123", "Mozilla/5.0 Chrome", "88.240.10.15");
        var hashDifferent = service.HashDeviceId("different_uuid_999", "Mozilla/5.0 Chrome", "88.240.10.15");

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, hashDifferent);
        Assert.Equal(64, hash1.Length); // 64 hex characters = SHA256
    }

    [Fact]
    public async Task Scenario11_RetryAfterCalculation_CalculatesAccurateResetTimestamp()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 0, "device_hash_1", CancellationToken.None);

        var result = await quotaService.TryConsumeCreditAsync(userId, "system", "device_1", "127.0.0.1", CancellationToken.None);
        Assert.False(result.Allowed);
        Assert.NotNull(result.ResetAt);
        Assert.True(result.ResetAt.Value > DateTime.UtcNow);
        Assert.True(result.ResetAt.Value <= DateTime.UtcNow.AddDays(1).AddMinutes(5));
    }

    [Fact]
    public async Task Scenario12_AbuseAuditLogging_RecordsEventsCorrectly()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var deviceRepo = new DeviceRegistrationRepository(db);
        var auditRepo = new AbuseAuditLogRepository(db);
        var config = CreateMockConfiguration();
        var logger = new Mock<ILogger<AbuseDetectionService>>();

        var service = new AbuseDetectionService(deviceRepo, auditRepo, db, logger.Object, config);

        await service.LogAbuseEventAsync(
            userId: Guid.NewGuid(),
            deviceHash: "test_device_hash",
            ipAddress: "192.168.1.100",
            eventType: "DISPOSABLE_EMAIL_BLOCKED",
            severity: "HIGH",
            riskScore: 50,
            details: "Disposable email attempt blocked."
        );

        var logs = await auditRepo.GetRecentLogsByDeviceHashAsync("test_device_hash");
        Assert.Single(logs);
        Assert.Equal("DISPOSABLE_EMAIL_BLOCKED", logs[0].EventType);
        Assert.Equal("HIGH", logs[0].Severity);
        Assert.Equal(50, logs[0].RiskScore);
    }

    [Fact]
    public async Task Scenario13_LegitimateUser_AcrossDifferentIPs_NotFalselyBanned()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var quotaRepo = new UserQuotaRepository(db);
        var userRepo = new UserRepository(db);
        var keyRepo = new UserAiKeyRepository(db);
        var abuseDetection = new Mock<IAbuseDetectionService>();
        var logger = new Mock<ILogger<QuotaService>>();

        var quotaService = new QuotaService(quotaRepo, userRepo, keyRepo, abuseDetection.Object, db, logger.Object);

        var userId = Guid.NewGuid();
        await quotaService.InitializeUserQuotaAsync(userId, 15, "home_device", CancellationToken.None);

        // Home IP
        var res1 = await quotaService.TryConsumeCreditAsync(userId, "system", "home_device", "192.168.1.10", CancellationToken.None);
        Assert.True(res1.Allowed);

        // Mobile 4G IP
        var res2 = await quotaService.TryConsumeCreditAsync(userId, "system", "home_device", "212.156.40.100", CancellationToken.None);
        Assert.True(res2.Allowed);

        // Office WiFi IP
        var res3 = await quotaService.TryConsumeCreditAsync(userId, "system", "home_device", "10.0.5.20", CancellationToken.None);
        Assert.True(res3.Allowed);

        var status = await quotaService.GetQuotaStatusAsync(userId, CancellationToken.None);
        Assert.Equal(3, status.UsedCredits);
        Assert.Equal(12, status.RemainingCredits);
    }
}
