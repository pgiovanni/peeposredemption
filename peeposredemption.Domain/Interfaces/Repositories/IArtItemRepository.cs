using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IArtItemRepository
{
    Task AddAsync(ArtItem item);
    Task<ArtItem?> GetByIdAsync(Guid id);
    Task<List<ArtItem>> GetByArtistIdAsync(Guid artistId);
    Task<List<ArtItem>> GetActiveAsync();
}
