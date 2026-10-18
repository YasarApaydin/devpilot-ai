using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Conversation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Include(c => c.Messages)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.Id == id && c.UserId == userId && !c.IsDeleted)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
