using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserDeviceRepository
{
    Task<UserDevice?> GetAsync(Guid deviceId, Guid userId);
    Task<List<UserDevice>> GetByDeviceIdAsync(Guid deviceId);
    Task<List<UserDevice>> GetByUserIdAsync(Guid userId);
    Task<bool> IsDeviceBannedAsync(Guid deviceId);
    Task<HashSet<Guid>> GetAllBannedDeviceIdsAsync();
    Task AddAsync(UserDevice device);
}
