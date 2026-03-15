using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ArtistCommissionRepository : IArtistCommissionRepository
{
    private readonly AppDbContext _db;
    public ArtistCommissionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ArtistCommission commission) =>
        await _db.ArtistCommissions.AddAsync(commission);

    public Task<List<ArtistCommission>> GetByArtistIdAsync(Guid artistId, int count) =>
        _db.ArtistCommissions
            .Where(c => c.ArtistId == artistId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync();

    public Task<long> GetTotalEarnedCentsAsync(Guid artistId) =>
        _db.ArtistCommissions
            .Where(c => c.ArtistId == artistId)
            .SumAsync(c => c.CommissionCents);

    public Task<List<ArtistCommission>> GetByArtItemIdAsync(Guid artItemId) =>
        _db.ArtistCommissions
            .Where(c => c.ArtItemId == artItemId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
}
