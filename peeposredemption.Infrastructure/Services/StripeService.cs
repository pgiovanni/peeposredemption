using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace peeposredemption.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        public StripeService(IConfiguration config)
        {
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        public async Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(
            Guid serverId, string serverName, StorageTier targetTier, string successUrl, string cancelUrl)
        {
            var (price, name, description) = targetTier switch
            {
                StorageTier.Standard => (199L, $"Standard Tier — {serverName}", "Increases emoji limit to 150 for this server. One-time payment."),
                StorageTier.Boosted  => (499L, $"Boosted Tier — {serverName}", "Increases emoji limit to 500 for this server. One-time payment."),
                _                    => throw new ArgumentException("Invalid upgrade tier.")
            };

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = price,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = name,
                                Description = description
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "serverId", serverId.ToString() },
                    { "targetTier", ((int)targetTier).ToString() }
                },
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return new StripeCheckoutResult(session.Id, session.Url);
        }

        public async Task<StripeCheckoutResult> CreateOrbPurchaseSessionAsync(
            Guid userId, int orbAmount, long priceCents, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = priceCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{orbAmount} Orbs",
                                Description = $"Purchase {orbAmount} orbs for your Torvex account."
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "type", "orb_purchase" },
                    { "userId", userId.ToString() }
                },
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return new StripeCheckoutResult(session.Id, session.Url);
        }
    }
}
