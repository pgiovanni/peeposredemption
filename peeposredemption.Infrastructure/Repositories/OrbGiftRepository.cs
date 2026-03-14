using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class OrbGiftRepository : IOrbGiftRepository
{
    private readonly AppDbContext _db;
    public OrbGiftRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(OrbGift gift) =>
        await _db.OrbGifts.AddAsync(gift);

    public Task<List<OrbGift>> GetRecentByChannelAsync(Guid channelId, int count) =>
        _db.OrbGifts
            .Where(g => g.ChannelId == channelId)
            .OrderByDescending(g => g.CreatedAt)
            .Take(count)
            .ToListAsync();
}
