using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task HardDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(_context.Database))
        {
            var existingTx = _context.Database.CurrentTransaction;
            var tx = existingTx == null ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                // Explicitly clean up all user-related data in strict dependency order to prevent any foreign key violation
                // 1. Documents & Chunks
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM document_chunks WHERE document_id IN (SELECT id FROM documents WHERE user_id = {userId})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM documents WHERE user_id = {userId}", cancellationToken);

                // 2. Security Scans, Consents & Findings
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM security_scan_findings WHERE security_scan_id IN (SELECT id FROM security_scans WHERE user_id = {userId})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM security_scan_consents WHERE user_id = {userId} OR security_scan_id IN (SELECT id FROM security_scans WHERE user_id = {userId})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM security_scans WHERE user_id = {userId}", cancellationToken);

                // 3. Conversations & Messages
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM messages WHERE user_id = {userId} OR conversation_id IN (SELECT id FROM conversations WHERE user_id = {userId})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM conversations WHERE user_id = {userId}", cancellationToken);

                // 4. User Quotas, AI Keys, Tokens & Logs
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM user_ai_keys WHERE user_id = {userId}", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM user_quotas WHERE user_id = {userId}", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM refresh_tokens WHERE user_id = {userId}", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM email_verification_tokens WHERE user_id = {userId}", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM abuse_audit_logs WHERE user_id = {userId}", cancellationToken);

                // 5. Finally delete user record
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM users WHERE id = {userId}", cancellationToken);

                if (tx != null)
                {
                    await tx.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (tx != null)
                {
                    await tx.RollbackAsync(cancellationToken);
                }
                throw;
            }
        }
        else
        {
            // Non-relational (InMemory) fallback for unit tests: clean up entities in strict dependency order
            var docIds = await _context.Documents.Where(d => d.UserId == userId).Select(d => d.Id).ToListAsync(cancellationToken);
            var chunks = await _context.DocumentChunks.Where(c => docIds.Contains(c.DocumentId)).ToListAsync(cancellationToken);
            _context.DocumentChunks.RemoveRange(chunks);

            var docs = await _context.Documents.Where(d => d.UserId == userId).ToListAsync(cancellationToken);
            _context.Documents.RemoveRange(docs);

            var scanIds = await _context.SecurityScans.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(cancellationToken);
            var findings = await _context.SecurityScanFindings.Where(f => scanIds.Contains(f.SecurityScanId)).ToListAsync(cancellationToken);
            _context.SecurityScanFindings.RemoveRange(findings);

            var consents = await _context.SecurityScanConsents.Where(c => c.UserId == userId || scanIds.Contains(c.SecurityScanId)).ToListAsync(cancellationToken);
            _context.SecurityScanConsents.RemoveRange(consents);

            var scans = await _context.SecurityScans.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
            _context.SecurityScans.RemoveRange(scans);

            var convIds = await _context.Conversations.Where(c => c.UserId == userId).Select(c => c.Id).ToListAsync(cancellationToken);
            var msgs = await _context.Messages.Where(m => m.UserId == userId || convIds.Contains(m.ConversationId)).ToListAsync(cancellationToken);
            _context.Messages.RemoveRange(msgs);

            var convs = await _context.Conversations.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
            _context.Conversations.RemoveRange(convs);

            var keys = await _context.UserAiKeys.Where(k => k.UserId == userId).ToListAsync(cancellationToken);
            _context.UserAiKeys.RemoveRange(keys);

            var quotas = await _context.UserQuotas.Where(q => q.UserId == userId).ToListAsync(cancellationToken);
            _context.UserQuotas.RemoveRange(quotas);

            var refreshTokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
            _context.RefreshTokens.RemoveRange(refreshTokens);

            var verifyTokens = await _context.EmailVerificationTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
            _context.EmailVerificationTokens.RemoveRange(verifyTokens);

            var abuseLogs = await _context.AbuseAuditLogs.Where(a => a.UserId == userId).ToListAsync(cancellationToken);
            _context.AbuseAuditLogs.RemoveRange(abuseLogs);

            var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user != null)
            {
                _dbSet.Remove(user);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
