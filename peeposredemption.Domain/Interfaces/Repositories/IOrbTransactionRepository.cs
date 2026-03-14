using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IOrbTransactionRepository
{
    Task AddAsync(OrbTransaction transaction);
    Task<long> GetBalanceAsync(Guid userId);
    Task<List<OrbTransaction>> GetRecentAsync(Guid userId, int count);
}
