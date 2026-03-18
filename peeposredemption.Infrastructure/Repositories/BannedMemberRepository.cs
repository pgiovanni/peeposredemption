using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class BannedMemberRepository : IBannedMemberRepository
    {
        private readonly AppDbContext _db;
        public BannedMemberRepository(AppDbContext db) => _db = db;

        public Task<bool> IsBannedAsync(Guid serverId, Guid userId) =>
            _db.BannedMembers.AnyAsync(b => b.ServerId == serverId && b.UserId == userId);

        public async Task AddAsync(BannedMember ban) =>
            await _db.BannedMembers.AddAsync(ban);

        public Task<BannedMember?> GetAsync(Guid serverId, Guid userId) =>
            _db.BannedMembers.FirstOrDefaultAsync(b => b.ServerId == serverId && b.UserId == userId);

        public Task<List<BannedMember>> GetByServerAsync(Guid serverId) =>
            _db.BannedMembers
                .Include(b => b.User)
                .Include(b => b.BannedBy)
                .Where(b => b.ServerId == serverId)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();

        public Task<List<BannedMember>> GetAllAsync() =>
            _db.BannedMembers
                .Include(b => b.User)
                .Include(b => b.BannedBy)
                .Include(b => b.Server)
                .OrderByDescending(b => b.BannedAt)
                .ToListAsync();

        public void Remove(BannedMember ban) =>
            _db.BannedMembers.Remove(ban);
    }
}
