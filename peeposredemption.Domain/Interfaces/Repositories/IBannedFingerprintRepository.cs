using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IBannedFingerprintRepository
{
    Task<bool> IsBannedAsync(string fingerprintHash);
    Task AddAsync(BannedFingerprint ban);
    Task<List<BannedFingerprint>> GetByHashAsync(string fingerprintHash);
}
