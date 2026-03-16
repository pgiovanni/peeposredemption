using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public Task<int> GetUnreadCountAsync(Guid userId) =>
            _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task<Dictionary<Guid, int>> GetUnreadCountByServerAsync(Guid userId)
        {
            var rows = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && n.ServerId != null)
                .GroupBy(n => n.ServerId!.Value)
                .Select(g => new { ServerId = g.Key, Count = g.Count() })
                .ToListAsync();
            return rows.ToDictionary(r => r.ServerId, r => r.Count);
        }

        public async Task<List<Notification>> GetRecentAsync(Guid userId, int count = 25) =>
            await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Include(n => n.FromUser)
                .ToListAsync();

        public async Task AddAsync(Notification notification) =>
            await _db.Notifications.AddAsync(notification);

        public async Task MarkAllReadAsync(Guid userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread) n.IsRead = true;
        }

        public async Task MarkServerNotificationsReadAsync(Guid userId, Guid serverId)
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && n.ServerId == serverId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread) n.IsRead = true;
        }
    }
}
