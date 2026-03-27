using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IAltSuspicionRepository
{
    Task<List<AltSuspicion>> GetPendingAsync();
    Task<AltSuspicion?> GetByIdAsync(Guid id);
    Task<AltSuspicion?> GetByUserPairAsync(Guid userId1, Guid userId2);
    Task AddAsync(AltSuspicion suspicion);
}
