using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _db;
        public MessageRepository(AppDbContext db) => _db = db;

        public Task<List<Message>> GetChannelMessagesAsync(
            Guid channelId, int page, int pageSize) =>
            _db.Messages.Include(m => m.Author)
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task AddAsync(Message message) => await _db.Messages.AddAsync(message);

        public Task<Message?> GetByIdAsync(Guid messageId) =>
            _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
    }

}
