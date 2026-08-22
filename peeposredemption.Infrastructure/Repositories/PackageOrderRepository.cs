using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class PackageOrderRepository : IPackageOrderRepository
{
    private readonly AppDbContext _db;
    public PackageOrderRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PackageOrder order) =>
        await _db.PackageOrders.AddAsync(order);

    public Task<PackageOrder?> GetByIdAsync(Guid id) =>
        _db.PackageOrders.FirstOrDefaultAsync(o => o.Id == id);

    public Task<PackageOrder?> GetBySessionIdAsync(string sessionId) =>
        _db.PackageOrders.FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);

    public Task<List<PackageOrder>> GetByUserAsync(Guid userId) =>
        _db.PackageOrders.Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<PackageOrder>> GetUnfulfilledAsync(string packageSlug) =>
        _db.PackageOrders
            .Where(o => o.PackageSlug == packageSlug
                     && o.Status == PurchaseStatus.Completed
                     && o.FulfilledAt == null)
            .OrderBy(o => o.PaidAt).ToListAsync();
}
