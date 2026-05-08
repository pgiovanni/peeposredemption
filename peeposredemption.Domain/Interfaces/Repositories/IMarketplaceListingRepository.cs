using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IMarketplaceListingRepository
{
    Task<MarketplaceListing?> GetByIdAsync(Guid id);
    Task<List<MarketplaceListing>> GetActiveByItemNameAsync(string itemName);
    Task<List<MarketplaceListing>> GetActiveBySellerIdAsync(Guid sellerId);
    Task<MarketplaceListing?> GetCheapestByItemNameAsync(string itemName);
    Task<List<MarketplaceListing>> GetAllActiveAsync();
    Task AddAsync(MarketplaceListing listing);
}
