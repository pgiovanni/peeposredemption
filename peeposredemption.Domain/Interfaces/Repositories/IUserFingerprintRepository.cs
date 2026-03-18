using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserFingerprintRepository
{
    Task<List<UserFingerprint>> GetByUserIdAsync(Guid userId);
    Task<List<UserFingerprint>> GetByFingerprintHashAsync(string hash);
    Task AddAsync(UserFingerprint fingerprint);
}
