using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IParentalLinkRepository
{
    Task AddAsync(ParentalLink link);
    Task<ParentalLink?> GetByIdAsync(Guid id);
    Task<ParentalLink?> GetByCodeAsync(string code);
    Task<ParentalLink?> GetActiveByChildIdAsync(Guid childUserId);
    Task<List<ParentalLink>> GetActiveByParentIdAsync(Guid parentUserId);
    Task<ParentalLink?> GetPendingByChildIdAsync(Guid childUserId);
}
