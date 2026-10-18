using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IEmailVerificationTokenRepository : IRepository<EmailVerificationToken>
{
    Task<EmailVerificationToken?> GetLatestActiveTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<EmailVerificationToken?> GetByCodeHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default);
    Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
