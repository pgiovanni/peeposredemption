using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserBadgeRepository
{
    Task<List<UserBadge>> GetByUserIdAsync(Guid userId);
    Task<bool> HasBadgeAsync(Guid userId, Guid badgeDefinitionId);
    Task AddAsync(UserBadge userBadge);
}
