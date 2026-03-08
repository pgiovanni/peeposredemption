using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using peeposredemption.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace peeposredemption.Infrastructure.Repositories
{
    public class ChannelRepository : IChannelRepository
    {
        private readonly AppDbContext _db;

        public ChannelRepository(AppDbContext db) => _db = db;

        public Task<Channel?> GetByIdAsync(Guid id) =>
            _db.Channels.FirstOrDefaultAsync(c => c.Id == id);

        public Task<List<Channel>> GetServerChannelsAsync(Guid serverId) =>
            _db.Channels
                .Where(c => c.ServerId == serverId)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task AddAsync(Channel channel) =>
            await _db.Channels.AddAsync(channel);

        public Task RemoveAsync(Channel channel)
        {
            _db.Channels.Remove(channel);
            return Task.CompletedTask;
        }
    }
}
