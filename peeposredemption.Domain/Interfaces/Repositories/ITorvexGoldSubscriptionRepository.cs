using peeposredemption.Domain.Entities;

namespace peeposredemption.Domain.Interfaces.Repositories;

public interface ITorvexGoldSubscriptionRepository
{
    Task<TorvexGoldSubscription?> GetByUserIdAsync(Guid userId);
    Task<TorvexGoldSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<TorvexGoldSubscription?> GetByStripeSessionIdAsync(string stripeSessionId);
    Task AddAsync(TorvexGoldSubscription subscription);
}
