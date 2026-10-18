using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class LoginAttemptService : ILoginAttemptService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);

    private record AttemptRecord(int Count, DateTime FirstAttempt, DateTime? LockoutUntil);
    private readonly ConcurrentDictionary<string, AttemptRecord> _attempts = new();
    private readonly ILogger<LoginAttemptService> _logger;

    public LoginAttemptService(ILogger<LoginAttemptService> logger)
    {
        _logger = logger;
    }

    private static string NormalizeKey(string ipAddress) => string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress.Trim();

    public bool IsBlocked(string ipAddress, out TimeSpan? remainingTime)
    {
        remainingTime = null;
        var key = NormalizeKey(ipAddress);

        if (_attempts.TryGetValue(key, out var record) && record.LockoutUntil.HasValue)
        {
            var now = DateTime.UtcNow;
            if (record.LockoutUntil.Value > now)
            {
                remainingTime = record.LockoutUntil.Value - now;
                return true;
            }
            _attempts.TryRemove(key, out _);
        }

        return false;
    }

    public void RecordFailedAttempt(string ipAddress, string? email = null)
    {
        var key = NormalizeKey(ipAddress);
        var now = DateTime.UtcNow;

        _attempts.AddOrUpdate(key,
            _ => new AttemptRecord(1, now, null),
            (_, existing) =>
            {
                if (now - existing.FirstAttempt > AttemptWindow && !existing.LockoutUntil.HasValue)
                {
                    return new AttemptRecord(1, now, null);
                }

                var newCount = existing.Count + 1;
                DateTime? lockoutUntil = null;

                if (newCount >= MaxFailedAttempts)
                {
                    lockoutUntil = now.Add(LockoutDuration);
                    _logger.LogWarning("IP {IpAddress} exceeded {MaxAttempts} failed login attempts for email '{Email}'. Locked until {LockoutUntil} UTC.",
                        ipAddress, MaxFailedAttempts, email ?? "unknown", lockoutUntil);
                }

                return new AttemptRecord(newCount, existing.FirstAttempt, lockoutUntil);
            });
    }

    public void ResetAttempts(string ipAddress, string? email = null)
    {
        var key = NormalizeKey(ipAddress);
        _attempts.TryRemove(key, out _);
    }

    public int GetFailedAttempts(string ipAddress)
    {
        var key = NormalizeKey(ipAddress);
        return _attempts.TryGetValue(key, out var record) ? record.Count : 0;
    }
}
