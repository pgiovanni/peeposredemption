using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Services
{
    public record StripeCheckoutResult(string SessionId, string Url);
    public record StripeInvoiceLinks(string InvoiceId, string? Number, string? HostedUrl, string? PdfUrl);

    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(Guid serverId, Guid userId, string serverName, StorageTier targetTier, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult> CreateOrbPurchaseSessionAsync(Guid userId, int orbAmount, long priceCents, string successUrl, string cancelUrl);
        Task<StripeCheckoutResult> CreateGoldSubscriptionSessionAsync(Guid userId, string successUrl, string cancelUrl);
        Task CancelSubscriptionAsync(string stripeSubscriptionId);

        /// <summary>Returns the Stripe Customer id for this user, creating one on first use.</summary>
        Task<string> GetOrCreateCustomerAsync(User user, string? fullName, string? company);

        /// <summary>
        /// One-time package checkout billed to a real Stripe Customer with invoice
        /// creation on, so every order produces a numbered, downloadable invoice.
        /// </summary>
        Task<StripeCheckoutResult> CreatePackageOrderSessionAsync(
            string customerId, Guid userId, Guid orderId, string packageName, string description,
            long priceCents, IDictionary<string, string> metadata, string successUrl, string cancelUrl);

        Task<StripeInvoiceLinks?> GetInvoiceLinksAsync(string invoiceId);
    }
}
