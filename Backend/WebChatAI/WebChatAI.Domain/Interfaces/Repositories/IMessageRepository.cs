using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IMessageRepository : IRepository<Message>
{
    Task<List<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
