using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserIpLogRepository : IUserIpLogRepository
{
    private readonly AppDbContext _db;
    public UserIpLogRepository(AppDbContext db) => _db = db;

    public Task<List<UserIpLog>> GetByUserIdAsync(Guid userId) =>
        _db.UserIpLogs.Where(l => l.UserId == userId).OrderByDescending(l => l.SeenAt).ToListAsync();

    public Task<List<UserIpLog>> GetByIpAddressAsync(string ipAddress) =>
        _db.UserIpLogs.Include(l => l.User).Where(l => l.IpAddress == ipAddress).OrderByDescending(l => l.SeenAt).ToListAsync();

    public async Task AddAsync(UserIpLog log) =>
        await _db.UserIpLogs.AddAsync(log);
}
