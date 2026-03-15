using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IArtistRepository
{
    Task AddAsync(Artist artist);
    Task<Artist?> GetByIdAsync(Guid id);
    Task<Artist?> GetByUserIdAsync(Guid userId);
    Task<List<Artist>> GetAllAsync();
    Task<int> CountAsync();
}
