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
                .OrderBy(sm => sm.SortOrder)
                .ThenBy(sm => sm.JoinedAt)
                .Select(sm => sm.Server)
                .ToListAsync();

        public async Task ReorderServersAsync(Guid userId, List<Guid> serverIds)
        {
            var members = await _db.ServerMembers
                .Where(sm => sm.UserId == userId && serverIds.Contains(sm.ServerId))
                .ToListAsync();

            for (int i = 0; i < serverIds.Count; i++)
            {
                var member = members.FirstOrDefault(m => m.ServerId == serverIds[i]);
                if (member != null) member.SortOrder = i;
            }
        }

        public Task<bool> IsMemberAsync(Guid serverId, Guid userId) =>
            _db.ServerMembers.AnyAsync(sm => sm.ServerId == serverId && sm.UserId == userId);

        public async Task AddAsync(Server server) =>
            await _db.Servers.AddAsync(server);

        public async Task AddMemberAsync(ServerMember member) =>
            await _db.ServerMembers.AddAsync(member);

        public Task<ServerMember?> GetMemberAsync(Guid serverId, Guid userId) =>
            _db.ServerMembers.FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);

        public async Task RemoveMemberAsync(Guid serverId, Guid userId)
        {
            var member = await GetMemberAsync(serverId, userId);
            if (member != null) _db.ServerMembers.Remove(member);
        }

        public Task<ServerRole?> GetMemberRoleAsync(Guid serverId, Guid userId) =>
            _db.ServerMembers
                .Where(sm => sm.ServerId == serverId && sm.UserId == userId)
                .Select(sm => (ServerRole?)sm.Role)
                .FirstOrDefaultAsync();

        public Task<List<ServerMember>> GetServerMembersAsync(Guid serverId) =>
            _db.ServerMembers
                .Include(sm => sm.User)
                .Where(sm => sm.ServerId == serverId)
                .ToListAsync();
    }
}
