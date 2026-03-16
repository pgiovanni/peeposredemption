using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IGameChannelConfigRepository
{
    Task<GameChannelConfig?> GetByChannelIdAsync(Guid channelId);
    Task<List<GameChannelConfig>> GetByServerAsync(Guid serverId);
    Task AddAsync(GameChannelConfig config);
}
