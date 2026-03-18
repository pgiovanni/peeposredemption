using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserFingerprintRepository : IUserFingerprintRepository
{
    private readonly AppDbContext _db;
    public UserFingerprintRepository(AppDbContext db) => _db = db;

    public Task<List<UserFingerprint>> GetByUserIdAsync(Guid userId) =>
        _db.UserFingerprints.Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt).ToListAsync();

    public Task<List<UserFingerprint>> GetByFingerprintHashAsync(string hash) =>
        _db.UserFingerprints.Include(f => f.User).Where(f => f.FingerprintHash == hash).ToListAsync();

    public async Task AddAsync(UserFingerprint fingerprint) =>
        await _db.UserFingerprints.AddAsync(fingerprint);
}
