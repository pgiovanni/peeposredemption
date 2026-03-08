using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class ModerationLogRepository : IModerationLogRepository
    {
        private readonly AppDbContext _db;
        public ModerationLogRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(ModerationLog entry) =>
            await _db.ModerationLogs.AddAsync(entry);

        public Task<List<ModerationLog>> GetServerLogsAsync(Guid serverId, int limit = 100) =>
            _db.ModerationLogs
                .Include(ml => ml.Moderator)
                .Include(ml => ml.TargetUser)
                .Where(ml => ml.ServerId == serverId)
                .OrderByDescending(ml => ml.CreatedAt)
                .Take(limit)
                .ToListAsync();
    }
}
