using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly AppDbContext _db;
    public ArtistRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Artist artist) =>
        await _db.Artists.AddAsync(artist);

    public Task<Artist?> GetByIdAsync(Guid id) =>
        _db.Artists.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Artist?> GetByUserIdAsync(Guid userId) =>
        _db.Artists.FirstOrDefaultAsync(a => a.UserId == userId);

    public Task<List<Artist>> GetAllAsync() =>
        _db.Artists.OrderBy(a => a.DisplayName).ToListAsync();

    public Task<int> CountAsync() =>
        _db.Artists.CountAsync();
}
