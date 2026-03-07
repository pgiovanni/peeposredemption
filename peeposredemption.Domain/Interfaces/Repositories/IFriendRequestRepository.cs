using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface IFriendRequestRepository
{
    Task AddAsync(FriendRequest request);
    Task<FriendRequest?> GetByIdAsync(Guid id);
    Task<List<FriendRequest>> GetPendingReceivedAsync(Guid userId);
    Task<List<FriendRequest>> GetAcceptedAsync(Guid userId);
    Task<bool> ExistsAsync(Guid senderId, Guid receiverId);
}
