using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Infrastructure.Repositories
{
    public class DirectMessageRepository : IDirectMessageRepository
    {
        private readonly AppDbContext _db;
        public DirectMessageRepository(AppDbContext db) => _db = db;

        public async Task<List<DirectMessage>> GetConversationAsync(
            Guid userA, Guid userB, int page, int pageSize)
        {
            var rows = await _db.DirectMessages
                .Where(dm =>
                    (dm.SenderId == userA && dm.RecipientId == userB) ||
                    (dm.SenderId == userB && dm.RecipientId == userA))
                .OrderByDescending(dm => dm.SentAt).ThenByDescending(dm => dm.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            rows.Reverse();
            return rows;
        }

        public async Task AddAsync(DirectMessage dm) => await _db.DirectMessages.AddAsync(dm);

        public Task<int> GetUnreadCountAsync(Guid userId) =>
            _db.DirectMessages.CountAsync(dm => dm.RecipientId == userId && !dm.IsRead);

        public async Task MarkConversationReadAsync(Guid recipientId, Guid senderId)
        {
            var unread = await _db.DirectMessages
                .Where(dm => dm.RecipientId == recipientId && dm.SenderId == senderId && !dm.IsRead)
                .ToListAsync();
            foreach (var dm in unread) dm.IsRead = true;
        }
    }

}
