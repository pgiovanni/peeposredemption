using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;
using peeposredemption.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace peeposredemption.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly string _goldPriceId;

        public StripeService(IConfiguration config)
        {
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
            _goldPriceId = config["Stripe:GoldPriceId"] ?? string.Empty;
        }

        public async Task<StripeCheckoutResult> CreateStorageUpgradeSessionAsync(
            Guid serverId, Guid userId, string serverName, StorageTier targetTier, string successUrl, string cancelUrl)
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
                    { "userId", userId.ToString() },
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

        public async Task<StripeCheckoutResult> CreateGoldSubscriptionSessionAsync(
            Guid userId, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = _goldPriceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "type", "gold" },
                    { "userId", userId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return new StripeCheckoutResult(session.Id, session.Url);
        }

        public async Task<string> GetOrCreateCustomerAsync(User user, string? fullName, string? company)
        {
            var customers = new CustomerService();
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                try
                {
                    var existing = await customers.GetAsync(user.StripeCustomerId);
                    if (existing != null && existing.Deleted != true) return existing.Id;
                }
                catch (StripeException) { /* stale id — fall through and recreate */ }
            }

            var created = await customers.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = string.IsNullOrWhiteSpace(fullName) ? user.DisplayOrUsername : fullName,
                Description = string.IsNullOrWhiteSpace(company) ? null : company,
                Metadata = new Dictionary<string, string> { { "torvexUserId", user.Id.ToString() } }
            });
            user.StripeCustomerId = created.Id;   // caller saves
            return created.Id;
        }

        public async Task<StripeCheckoutResult> CreatePackageOrderSessionAsync(
            string customerId, Guid userId, Guid orderId, string packageName, string description,
            long priceCents, IDictionary<string, string> metadata, string successUrl, string cancelUrl)
        {
            var meta = new Dictionary<string, string>(metadata)
            {
                ["type"] = "package_order",
                ["userId"] = userId.ToString(),
                ["orderId"] = orderId.ToString()
            };
            var options = new SessionCreateOptions
            {
                Customer = customerId,
                CustomerUpdate = new SessionCustomerUpdateOptions { Address = "auto", Name = "auto" },
                BillingAddressCollection = "required",
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
                                Name = packageName,
                                Description = description
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                // A real invoice for every order — numbered, hosted + PDF, emailed by
                // Stripe (dashboard "successful payments" email) and mirrored on /Dashboard.
                InvoiceCreation = new SessionInvoiceCreationOptions
                {
                    Enabled = true,
                    InvoiceData = new SessionInvoiceCreationInvoiceDataOptions
                    {
                        Description = description,
                        Metadata = meta,
                        Footer = "Torvex · torvex.app · Questions? admin@torvex.app"
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = meta,
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true }
            };

            var session = await new SessionService().CreateAsync(options);
            return new StripeCheckoutResult(session.Id, session.Url);
        }

        public async Task<StripeInvoiceLinks?> GetInvoiceLinksAsync(string invoiceId)
        {
            if (string.IsNullOrEmpty(invoiceId)) return null;
            var inv = await new InvoiceService().GetAsync(invoiceId);
            return inv == null ? null : new StripeInvoiceLinks(inv.Id, inv.Number, inv.HostedInvoiceUrl, inv.InvoicePdf);
        }

        public async Task CancelSubscriptionAsync(string stripeSubscriptionId)
        {
            var service = new SubscriptionService();
            await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });
        }
    }
}
