using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
        _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == tokenHash);

    public Task<RefreshToken?> GetByIdAsync(Guid id) =>
        _db.RefreshTokens.FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<RefreshToken>> GetActiveSessionsAsync(Guid userId) =>
        _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(RefreshToken refreshToken) =>
        await _db.RefreshTokens.AddAsync(refreshToken);

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();
        foreach (var t in tokens) t.IsRevoked = true;
    }

    public async Task RevokeByTokenAsync(string tokenHash)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == tokenHash && !r.IsRevoked);
        if (token != null) token.IsRevoked = true;
    }
}
