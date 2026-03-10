using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class ReferralRepository : IReferralRepository
{
    private readonly AppDbContext _db;
    public ReferralRepository(AppDbContext db) => _db = db;

    public Task<ReferralCode?> GetCodeByStringAsync(string code) =>
        _db.ReferralCodes.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Code == code);

    public Task<ReferralCode?> GetCodeByOwnerIdAsync(Guid ownerId) =>
        _db.ReferralCodes.FirstOrDefaultAsync(r => r.OwnerId == ownerId);

    public Task<ReferralCode?> GetCodeByIdAsync(Guid id) =>
        _db.ReferralCodes.FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddCodeAsync(ReferralCode code)
    {
        _db.ReferralCodes.Add(code);
        await Task.CompletedTask;
    }

    public async Task AddPurchaseAsync(ReferralPurchase purchase)
    {
        _db.ReferralPurchases.Add(purchase);
        await Task.CompletedTask;
    }

    public Task<int> GetReferredUserCountAsync(Guid codeId) =>
        _db.Users.CountAsync(u => u.ReferredByCodeId == codeId);

    public Task<List<ReferralPurchase>> GetPurchasesByCodeIdAsync(Guid codeId) =>
        _db.ReferralPurchases.Where(p => p.ReferralCodeId == codeId).ToListAsync();

    public Task<List<ReferralCode>> GetAllCodesAsync() =>
        _db.ReferralCodes.Include(r => r.Owner).Include(r => r.Purchases).ToListAsync();

    public Task<bool> PurchaseExistsAsync(string stripeSessionId) =>
        _db.ReferralPurchases.AnyAsync(p => p.StripeSessionId == stripeSessionId);
}
