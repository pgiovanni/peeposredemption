using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IArtistCommissionRepository
{
    Task AddAsync(ArtistCommission commission);
    Task<List<ArtistCommission>> GetByArtistIdAsync(Guid artistId, int count);
    Task<long> GetTotalEarnedCentsAsync(Guid artistId);
    Task<List<ArtistCommission>> GetByArtItemIdAsync(Guid artItemId);
}
