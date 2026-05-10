using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IGuildConfigRepository
{
    Task<GuildConfig?> GetByGuildIdAsync(string guildId);
    Task AddAsync(GuildConfig config);
}
