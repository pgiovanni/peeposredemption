using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class DiscordLinkRepository : IDiscordLinkRepository
{
    private readonly AppDbContext _db;
    public DiscordLinkRepository(AppDbContext db) => _db = db;

    public Task<DiscordLink?> GetByDiscordIdAsync(string discordUserId) =>
        _db.DiscordLinks.FirstOrDefaultAsync(l => l.DiscordUserId == discordUserId);

    public Task<DiscordLink?> GetByUserIdAsync(Guid torvexUserId) =>
        _db.DiscordLinks.FirstOrDefaultAsync(l => l.TorvexUserId == torvexUserId);

    public async Task AddAsync(DiscordLink link) =>
        await _db.DiscordLinks.AddAsync(link);
}
