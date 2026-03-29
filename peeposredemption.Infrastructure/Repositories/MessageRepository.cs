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

        public async Task<List<Message>> GetChannelMessagesAsync(
            Guid channelId, int page, int pageSize)
        {
            // Fetch newest N descending, then reverse to ascending for display
            var rows = await _db.Messages
                .Include(m => m.Author)
                .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Author)
                .Include(m => m.Attachments)
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            rows.Reverse();
            return rows;
        }

        public async Task AddAsync(Message message) => await _db.Messages.AddAsync(message);

        public Task<Message?> GetByIdAsync(Guid messageId) =>
            _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

        public async Task<int[]> GetHourlyActivityAsync(Guid userId)
        {
            var counts = await _db.Messages
                .Where(m => m.AuthorId == userId && !m.IsDeleted)
                .GroupBy(m => m.SentAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new int[24];
            foreach (var c in counts) result[c.Hour] = c.Count;
            return result;
        }
    }

}
