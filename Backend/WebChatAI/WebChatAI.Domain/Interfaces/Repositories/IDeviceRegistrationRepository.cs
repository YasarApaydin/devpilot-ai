using WebChatAI.Domain.Entities;

namespace WebChatAI.Domain.Interfaces.Repositories;

public interface IDeviceRegistrationRepository : IRepository<DeviceRegistration>
{
    Task<DeviceRegistration?> GetByDeviceHashAsync(string deviceHash, CancellationToken cancellationToken = default);
}
