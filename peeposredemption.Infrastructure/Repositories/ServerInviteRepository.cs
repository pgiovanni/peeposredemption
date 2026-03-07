using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class ServerInviteRepository : IServerInviteRepository
    {
        private readonly AppDbContext _db;
        public ServerInviteRepository(AppDbContext db) => _db = db;

        public Task<ServerInvite?> GetByCodeAsync(string code) =>
            _db.ServerInvites.Include(i => i.Server).FirstOrDefaultAsync(i => i.Code == code);

        public async Task AddAsync(ServerInvite invite) =>
            await _db.ServerInvites.AddAsync(invite);
    }
}
