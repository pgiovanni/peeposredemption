using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserBadgeRepository : IUserBadgeRepository
{
    private readonly AppDbContext _db;
    public UserBadgeRepository(AppDbContext db) => _db = db;

    public Task<List<UserBadge>> GetByUserIdAsync(Guid userId) =>
        _db.UserBadges
            .Include(ub => ub.BadgeDefinition)
            .Where(ub => ub.UserId == userId)
            .OrderBy(ub => ub.BadgeDefinition.SortOrder)
            .ToListAsync();

    public Task<bool> HasBadgeAsync(Guid userId, Guid badgeDefinitionId) =>
        _db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeDefinitionId == badgeDefinitionId);

    public async Task AddAsync(UserBadge userBadge) =>
        await _db.UserBadges.AddAsync(userBadge);
}
