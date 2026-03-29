using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Services
{
    public record StripeCheckoutResult(string SessionId, string Url);

    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(Guid serverId, Guid userId, string serverName, StorageTier targetTier, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult> CreateOrbPurchaseSessionAsync(Guid userId, int orbAmount, long priceCents, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult> CreateGoldSubscriptionSessionAsync(Guid userId, string successUrl, string cancelUrl);
        Task CancelSubscriptionAsync(string stripeSubscriptionId);
    }
}
