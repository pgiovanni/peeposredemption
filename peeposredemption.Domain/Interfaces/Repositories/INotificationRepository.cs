using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<int> GetUnreadCountAsync(Guid userId);
        Task AddAsync(Notification notification);
        Task MarkAllReadAsync(Guid userId);
    }
}
