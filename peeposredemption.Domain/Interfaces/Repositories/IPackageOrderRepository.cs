using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IPackageOrderRepository
{
    Task AddAsync(PackageOrder order);
    Task<PackageOrder?> GetByIdAsync(Guid id);
    Task<PackageOrder?> GetBySessionIdAsync(string sessionId);
    Task<List<PackageOrder>> GetByUserAsync(Guid userId);
    /// <summary>Paid orders the bot hasn't applied yet (AI credit grants awaiting fulfillment).</summary>
    Task<List<PackageOrder>> GetUnfulfilledAsync(string packageSlug);
}
