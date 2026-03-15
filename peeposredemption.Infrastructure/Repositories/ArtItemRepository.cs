using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ArtItemRepository : IArtItemRepository
{
    private readonly AppDbContext _db;
    public ArtItemRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ArtItem item) =>
        await _db.ArtItems.AddAsync(item);

    public Task<ArtItem?> GetByIdAsync(Guid id) =>
        _db.ArtItems.FirstOrDefaultAsync(i => i.Id == id);

    public Task<List<ArtItem>> GetByArtistIdAsync(Guid artistId) =>
        _db.ArtItems.Where(i => i.ArtistId == artistId).OrderBy(i => i.Name).ToListAsync();

    public Task<List<ArtItem>> GetActiveAsync() =>
        _db.ArtItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
}
