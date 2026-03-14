using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IUserLoginStreakRepository
{
    Task<UserLoginStreak?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserLoginStreak streak);
}
