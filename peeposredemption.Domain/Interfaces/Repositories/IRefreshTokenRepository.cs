using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<RefreshToken?> GetByIdAsync(Guid id);
    Task<List<RefreshToken>> GetActiveSessionsAsync(Guid userId);
    Task AddAsync(RefreshToken refreshToken);
    Task RevokeAllForUserAsync(Guid userId);
    Task RevokeByTokenAsync(string tokenHash);
}
