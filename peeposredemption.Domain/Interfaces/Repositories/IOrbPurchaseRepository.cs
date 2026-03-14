using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IOrbPurchaseRepository
{
    Task AddAsync(OrbPurchase purchase);
    Task<OrbPurchase?> GetBySessionIdAsync(string sessionId);
}
