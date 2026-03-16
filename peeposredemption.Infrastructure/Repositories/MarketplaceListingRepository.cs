using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class MarketplaceListingRepository : IMarketplaceListingRepository
{
    private readonly AppDbContext _db;
    public MarketplaceListingRepository(AppDbContext db) => _db = db;

    public Task<MarketplaceListing?> GetByIdAsync(Guid id) =>
        _db.MarketplaceListings
            .Include(l => l.ItemDefinition)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == id);

    public Task<List<MarketplaceListing>> GetActiveByItemNameAsync(string itemName) =>
        _db.MarketplaceListings
            .Include(l => l.ItemDefinition)
            .Include(l => l.Seller).ThenInclude(s => s.User)
            .Where(l => l.Status == MarketListingStatus.Active
                && l.ItemDefinition.Name.ToLower().Contains(itemName.ToLower())
                && l.ExpiresAt > DateTime.UtcNow)
            .OrderBy(l => l.PricePerUnit)
            .ToListAsync();

    public Task<List<MarketplaceListing>> GetActiveBySellerIdAsync(Guid sellerId) =>
        _db.MarketplaceListings
            .Include(l => l.ItemDefinition)
            .Where(l => l.SellerId == sellerId && l.Status == MarketListingStatus.Active)
            .ToListAsync();

    public Task<MarketplaceListing?> GetCheapestByItemNameAsync(string itemName) =>
        _db.MarketplaceListings
            .Include(l => l.ItemDefinition)
            .Include(l => l.Seller).ThenInclude(s => s.User)
            .Where(l => l.Status == MarketListingStatus.Active
                && l.ItemDefinition.Name.ToLower() == itemName.ToLower()
                && l.ExpiresAt > DateTime.UtcNow)
            .OrderBy(l => l.PricePerUnit)
            .FirstOrDefaultAsync();

    public async Task AddAsync(MarketplaceListing listing) =>
        await _db.MarketplaceListings.AddAsync(listing);
}
