using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure.Repositories
{
    public class ServerRepository : IServerRepository
    {
        private readonly AppDbContext _db;

        public ServerRepository(AppDbContext db) => _db = db;

        public Task<Server?> GetByIdAsync(Guid id) =>
            _db.Servers.FirstOrDefaultAsync(s => s.Id == id);

        public Task<List<Server>> GetUserServersAsync(Guid userId) =>
            _db.ServerMembers
                .Where(sm => sm.UserId == userId)
                .Select(sm => sm.Server)
                .ToListAsync();

        public Task<bool> IsMemberAsync(Guid serverId, Guid userId) =>
            _db.ServerMembers.AnyAsync(sm => sm.ServerId == serverId && sm.UserId == userId);

        public async Task AddAsync(Server server) =>
            await _db.Servers.AddAsync(server);

        public async Task AddMemberAsync(ServerMember member) =>
            await _db.ServerMembers.AddAsync(member);
    }
}
