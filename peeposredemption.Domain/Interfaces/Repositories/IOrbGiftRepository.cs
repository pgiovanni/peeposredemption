using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IOrbGiftRepository
{
    Task AddAsync(OrbGift gift);
    Task<List<OrbGift>> GetRecentByChannelAsync(Guid channelId, int count);
}
