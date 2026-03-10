namespace peeposredemption.Application.Services
{
    public record StripeCheckoutResult(string SessionId, string Url);

    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(Guid serverId, string serverName, string successUrl, string cancelUrl);
    }
}
