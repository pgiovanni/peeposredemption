using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IFeatureRequestRepository
{
    Task AddAsync(FeatureRequest featureRequest);
    Task<List<FeatureRequest>> GetByUserIdAsync(Guid userId);
    Task<List<FeatureRequest>> GetAllAsync();
    Task<FeatureRequest?> GetByIdAsync(Guid id);
}
