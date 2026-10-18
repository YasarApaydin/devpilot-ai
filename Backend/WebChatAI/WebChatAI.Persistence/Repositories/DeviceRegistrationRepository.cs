using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;
using WebChatAI.Persistence.Context;

namespace WebChatAI.Persistence.Repositories;

public class DeviceRegistrationRepository : Repository<DeviceRegistration>, IDeviceRegistrationRepository
{
    public DeviceRegistrationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DeviceRegistration?> GetByDeviceHashAsync(string deviceHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.DeviceHash == deviceHash && !d.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
