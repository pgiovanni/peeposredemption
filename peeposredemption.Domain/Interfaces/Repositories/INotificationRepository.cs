using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<Dictionary<Guid, int>> GetUnreadCountByServerAsync(Guid userId);
        Task<List<Notification>> GetRecentAsync(Guid userId, int count = 25);
        Task AddAsync(Notification notification);
        Task MarkAllReadAsync(Guid userId);
        Task MarkServerNotificationsReadAsync(Guid userId, Guid serverId);
    }
}
