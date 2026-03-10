using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface IStorageUpgradePurchaseRepository
    {
        Task<StorageUpgradePurchase?> GetBySessionIdAsync(string sessionId);
        Task AddAsync(StorageUpgradePurchase purchase);
    }
}
