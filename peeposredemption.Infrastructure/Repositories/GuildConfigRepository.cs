using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class GuildConfigRepository : IGuildConfigRepository
{
    private readonly AppDbContext _db;
    public GuildConfigRepository(AppDbContext db) => _db = db;

    public Task<GuildConfig?> GetByGuildIdAsync(string guildId) =>
        _db.GuildConfigs.FirstOrDefaultAsync(g => g.GuildId == guildId);

    public async Task AddAsync(GuildConfig config) =>
        await _db.GuildConfigs.AddAsync(config);
}
