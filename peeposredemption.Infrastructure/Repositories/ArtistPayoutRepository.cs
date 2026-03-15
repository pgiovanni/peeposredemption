using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ArtistPayoutRepository : IArtistPayoutRepository
{
    private readonly AppDbContext _db;
    public ArtistPayoutRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ArtistPayout payout) =>
        await _db.ArtistPayouts.AddAsync(payout);

    public Task<List<ArtistPayout>> GetByArtistIdAsync(Guid artistId) =>
        _db.ArtistPayouts
            .Where(p => p.ArtistId == artistId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

    public Task<long> GetTotalPaidCentsAsync(Guid artistId) =>
        _db.ArtistPayouts
            .Where(p => p.ArtistId == artistId)
            .SumAsync(p => p.AmountCents);
}
