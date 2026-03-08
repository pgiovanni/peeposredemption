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
    }
}
