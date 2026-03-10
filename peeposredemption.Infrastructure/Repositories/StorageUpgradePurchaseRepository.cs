using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class StorageUpgradePurchaseRepository : IStorageUpgradePurchaseRepository
    {
        private readonly AppDbContext _db;

        public StorageUpgradePurchaseRepository(AppDbContext db) => _db = db;

        public Task<StorageUpgradePurchase?> GetBySessionIdAsync(string sessionId) =>
            _db.StorageUpgradePurchases.FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);

        public async Task AddAsync(StorageUpgradePurchase purchase) =>
            await _db.StorageUpgradePurchases.AddAsync(purchase);
    }
}
