using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class FriendRequestRepository : IFriendRequestRepository
{
    private readonly AppDbContext _db;
    public FriendRequestRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(FriendRequest request) =>
        await _db.FriendRequests.AddAsync(request);

    public Task<FriendRequest?> GetByIdAsync(Guid id) =>
        _db.FriendRequests
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<FriendRequest>> GetPendingReceivedAsync(Guid userId) =>
        _db.FriendRequests
            .Include(r => r.Sender)
            .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
            .ToListAsync();

    public Task<List<FriendRequest>> GetAcceptedAsync(Guid userId) =>
        _db.FriendRequests
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                        && r.Status == FriendRequestStatus.Accepted)
            .ToListAsync();

    public Task<bool> ExistsAsync(Guid senderId, Guid receiverId) =>
        _db.FriendRequests.AnyAsync(r =>
            (r.SenderId == senderId && r.ReceiverId == receiverId) ||
            (r.SenderId == receiverId && r.ReceiverId == senderId));
}
