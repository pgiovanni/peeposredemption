using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserActivityStatsRepository
{
    Task<UserActivityStats?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserActivityStats stats);
}
