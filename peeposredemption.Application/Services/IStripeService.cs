using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Services
{
    public record StripeCheckoutResult(string SessionId, string Url);

    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(Guid serverId, string serverName, StorageTier targetTier, string successUrl, string cancelUrl);
    }
}
