using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces.Repositories;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.Infrastructure.Repositories;

public class TorvexGoldSubscriptionRepository : ITorvexGoldSubscriptionRepository
{
    private readonly AppDbContext _db;
    public TorvexGoldSubscriptionRepository(AppDbContext db) => _db = db;

    public Task<TorvexGoldSubscription?> GetByUserIdAsync(Guid userId) =>
        _db.GoldSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

    public Task<TorvexGoldSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId) =>
        _db.GoldSubscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

    public Task<TorvexGoldSubscription?> GetByStripeSessionIdAsync(string stripeSessionId) =>
        _db.GoldSubscriptions.FirstOrDefaultAsync(s => s.StripeSessionId == stripeSessionId);

    public async Task AddAsync(TorvexGoldSubscription subscription) =>
        await _db.GoldSubscriptions.AddAsync(subscription);
}
