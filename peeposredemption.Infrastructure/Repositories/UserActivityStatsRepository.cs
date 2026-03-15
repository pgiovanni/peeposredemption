using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class UserActivityStatsRepository : IUserActivityStatsRepository
{
    private readonly AppDbContext _db;
    public UserActivityStatsRepository(AppDbContext db) => _db = db;

    public Task<UserActivityStats?> GetByUserIdAsync(Guid userId) =>
        _db.UserActivityStats.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task AddAsync(UserActivityStats stats) =>
        await _db.UserActivityStats.AddAsync(stats);
}
