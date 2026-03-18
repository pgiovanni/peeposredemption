using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserDeviceRepository : IUserDeviceRepository
{
    private readonly AppDbContext _db;
    public UserDeviceRepository(AppDbContext db) => _db = db;

    public Task<UserDevice?> GetAsync(Guid deviceId, Guid userId) =>
        _db.UserDevices.FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

    public Task<List<UserDevice>> GetByDeviceIdAsync(Guid deviceId) =>
        _db.UserDevices.Include(d => d.User).Where(d => d.DeviceId == deviceId).ToListAsync();

    public Task<List<UserDevice>> GetByUserIdAsync(Guid userId) =>
        _db.UserDevices.Where(d => d.UserId == userId).ToListAsync();

    public Task<bool> IsDeviceBannedAsync(Guid deviceId) =>
        _db.UserDevices.AnyAsync(d => d.DeviceId == deviceId && d.IsBanned);

    public async Task<HashSet<Guid>> GetAllBannedDeviceIdsAsync()
    {
        var ids = await _db.UserDevices
            .Where(d => d.IsBanned)
            .Select(d => d.DeviceId)
            .Distinct()
            .ToListAsync();
        return new HashSet<Guid>(ids);
    }

    public async Task AddAsync(UserDevice device) =>
        await _db.UserDevices.AddAsync(device);
}
