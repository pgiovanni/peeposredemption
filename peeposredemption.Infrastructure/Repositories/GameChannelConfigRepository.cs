using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class GameChannelConfigRepository : IGameChannelConfigRepository
{
    private readonly AppDbContext _db;
    public GameChannelConfigRepository(AppDbContext db) => _db = db;

    public Task<GameChannelConfig?> GetByChannelIdAsync(Guid channelId) =>
        _db.GameChannelConfigs.FirstOrDefaultAsync(c => c.ChannelId == channelId);

    public Task<List<GameChannelConfig>> GetByServerAsync(Guid serverId) =>
        _db.GameChannelConfigs
            .Include(c => c.Channel)
            .Where(c => c.Channel.ServerId == serverId)
            .ToListAsync();

    public async Task AddAsync(GameChannelConfig config) =>
        await _db.GameChannelConfigs.AddAsync(config);
}
