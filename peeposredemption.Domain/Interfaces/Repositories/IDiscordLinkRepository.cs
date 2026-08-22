using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IDiscordLinkRepository
{
    Task<DiscordLink?> GetByDiscordIdAsync(string discordUserId);
    Task<DiscordLink?> GetByUserIdAsync(Guid torvexUserId);
    Task AddAsync(DiscordLink link);
}
