using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;
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
            Guid serverId, string serverName, string successUrl, string cancelUrl)
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
                            UnitAmount = 499, // $4.99
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Server Boost — {serverName}",
                                Description = "Increases emoji limit from 10 to 100 for this server."
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
                    { "serverId", serverId.ToString() }
                },
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return new StripeCheckoutResult(session.Id, session.Url);
        }
    }
}
