using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class BannedFingerprintRepository : IBannedFingerprintRepository
{
    private readonly AppDbContext _db;
    public BannedFingerprintRepository(AppDbContext db) => _db = db;

    public Task<bool> IsBannedAsync(string fingerprintHash) =>
        _db.BannedFingerprints.AnyAsync(b => b.FingerprintHash == fingerprintHash);

    public async Task AddAsync(BannedFingerprint ban) =>
        await _db.BannedFingerprints.AddAsync(ban);

    public Task<List<BannedFingerprint>> GetByHashAsync(string fingerprintHash) =>
        _db.BannedFingerprints.Where(b => b.FingerprintHash == fingerprintHash).ToListAsync();
}
