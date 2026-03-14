using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserLoginStreakRepository : IUserLoginStreakRepository
{
    private readonly AppDbContext _db;
    public UserLoginStreakRepository(AppDbContext db) => _db = db;

    public Task<UserLoginStreak?> GetByUserIdAsync(Guid userId) =>
        _db.UserLoginStreaks.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task AddAsync(UserLoginStreak streak) =>
        await _db.UserLoginStreaks.AddAsync(streak);
}
