using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IArtistPayoutRepository
{
    Task AddAsync(ArtistPayout payout);
    Task<List<ArtistPayout>> GetByArtistIdAsync(Guid artistId);
    Task<long> GetTotalPaidCentsAsync(Guid artistId);
}
