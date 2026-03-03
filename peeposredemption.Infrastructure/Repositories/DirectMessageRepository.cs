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

        public Task<List<DirectMessage>> GetConversationAsync(
            Guid userA, Guid userB, int page, int pageSize) =>
            _db.DirectMessages
                .Where(dm =>
                    (dm.SenderId == userA && dm.RecipientId == userB) ||
                    (dm.SenderId == userB && dm.RecipientId == userA))
                .OrderByDescending(dm => dm.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

        public async Task AddAsync(DirectMessage dm) => await _db.DirectMessages.AddAsync(dm);
    }

}
