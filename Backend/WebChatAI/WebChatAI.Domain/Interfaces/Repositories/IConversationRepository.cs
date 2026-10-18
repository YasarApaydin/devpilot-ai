using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
