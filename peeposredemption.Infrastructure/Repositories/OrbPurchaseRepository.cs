using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class OrbPurchaseRepository : IOrbPurchaseRepository
{
    private readonly AppDbContext _db;
    public OrbPurchaseRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(OrbPurchase purchase) =>
        await _db.OrbPurchases.AddAsync(purchase);

    public Task<OrbPurchase?> GetBySessionIdAsync(string sessionId) =>
        _db.OrbPurchases.FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
}
