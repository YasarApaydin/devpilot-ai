using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class UserQuotaRepository : Repository<UserQuota>, IUserQuotaRepository
{
    public UserQuotaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserQuota?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(q => q.UserId == userId && !q.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserQuota?> GetByUserIdNoTrackingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(q => q.UserId == userId && !q.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly SemaphoreSlim _inMemoryFallbackLock = new(1, 1);

    public async Task<bool> EnsureCycleResetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(_context.Database))
        {
            // Atomically rollover cycle if and only if CycleStartDate < today
            var rows = await _dbSet
                .Where(q => q.UserId == userId && !q.IsDeleted && q.CycleStartDate < today)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.CycleStartDate, today)
                    .SetProperty(q => q.CreditsUsedToday, 0),
                    cancellationToken);

            return rows > 0;
        }

        // Fallback for non-relational test providers (such as InMemoryDatabase)
        await _inMemoryFallbackLock.WaitAsync(cancellationToken);
        try
        {
            _context.ChangeTracker.Clear();
            var q = await _dbSet.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken);
            if (q != null && q.CycleStartDate < today)
            {
                q.CycleStartDate = today;
                q.CreditsUsedToday = 0;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
        finally
        {
            _inMemoryFallbackLock.Release();
        }
    }

    public async Task<bool> TryAtomicConsumeCreditAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(_context.Database))
        {
            // 1. First attempt: If cycle has elapsed (CycleStartDate < today), reset cycle and atomically consume 1st credit in a single statement
            var resetAndConsumed = await _dbSet
                .Where(q => q.UserId == userId && !q.IsDeleted && q.CycleStartDate < today && q.DailyCreditLimit > 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.CycleStartDate, today)
                    .SetProperty(q => q.CreditsUsedToday, 1)
                    .SetProperty(q => q.LastConsumedAt, DateTime.UtcNow),
                    cancellationToken);

            if (resetAndConsumed > 0)
            {
                return true;
            }

            // 2. Second attempt: Current cycle consumption
            var rowsAffected = await _dbSet
                .Where(q => q.UserId == userId && !q.IsDeleted && q.CycleStartDate >= today && q.CreditsUsedToday < q.DailyCreditLimit)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.CreditsUsedToday, q => q.CreditsUsedToday + 1)
                    .SetProperty(q => q.LastConsumedAt, DateTime.UtcNow),
                    cancellationToken);

            return rowsAffected > 0;
        }

        // Fallback for non-relational test providers (such as InMemoryDatabase)
        await _inMemoryFallbackLock.WaitAsync(cancellationToken);
        try
        {
            _context.ChangeTracker.Clear();
            var quota = await _dbSet.FirstOrDefaultAsync(q => q.UserId == userId && !q.IsDeleted, cancellationToken);
            if (quota == null)
            {
                return false;
            }

            if (quota.CycleStartDate < today)
            {
                if (quota.DailyCreditLimit <= 0)
                {
                    return false;
                }

                quota.CycleStartDate = today;
                quota.CreditsUsedToday = 1;
                quota.LastConsumedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            if (quota.CreditsUsedToday >= quota.DailyCreditLimit)
            {
                return false;
            }

            quota.CreditsUsedToday++;
            quota.LastConsumedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            _inMemoryFallbackLock.Release();
        }
    }

    public async Task<bool> TryRefundCreditAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(_context.Database))
        {
            var rowsAffected = await _dbSet
                .Where(q => q.UserId == userId && !q.IsDeleted && q.CreditsUsedToday > 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(q => q.CreditsUsedToday, q => q.CreditsUsedToday - 1),
                    cancellationToken);

            return rowsAffected > 0;
        }

        // Fallback for non-relational test providers (such as InMemoryDatabase)
        await _inMemoryFallbackLock.WaitAsync(cancellationToken);
        try
        {
            _context.ChangeTracker.Clear();
            var quota = await _dbSet.FirstOrDefaultAsync(q => q.UserId == userId && !q.IsDeleted, cancellationToken);
            if (quota == null || quota.CreditsUsedToday <= 0)
            {
                return false;
            }
            quota.CreditsUsedToday--;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            _inMemoryFallbackLock.Release();
        }
    }
}
