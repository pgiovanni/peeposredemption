using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class IpBanRepository : IIpBanRepository
{
    private readonly AppDbContext _db;
    public IpBanRepository(AppDbContext db) => _db = db;

    public Task<bool> IsBannedAsync(string ipAddress) =>
        _db.IpBans.AnyAsync(b => b.IpAddress == ipAddress
            && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow));

    public Task<List<IpBan>> GetAllAsync() =>
        _db.IpBans.Include(b => b.BannedBy).OrderByDescending(b => b.CreatedAt).ToListAsync();

    public async Task AddAsync(IpBan ban) =>
        await _db.IpBans.AddAsync(ban);

    public async Task RemoveAsync(Guid id)
    {
        var ban = await _db.IpBans.FindAsync(id);
        if (ban != null) _db.IpBans.Remove(ban);
    }
}
