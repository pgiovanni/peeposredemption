using Microsoft.Extensions.Configuration;
using peeposredemption.Application.Services;
using Stripe;
using Stripe.Checkout;

namespace peeposredemption.Infrastructure.Services
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly string _webhookSecret;

        public StripeWebhookService(IConfiguration config)
        {
            _webhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
        }

        public StripeWebhookEvent ParseAndVerify(string payload, string signature)
        {
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            }
            catch (StripeException ex)
            {
                throw new UnauthorizedAccessException("Invalid Stripe webhook signature.", ex);
            }

            string? sessionId = null;
            string? serverId = null;
            string? userId = null;
            long amountTotal = 0;
            string? subscriptionId = null;
            string? subscriptionStatus = null;
            DateTime? periodStart = null;

            if (stripeEvent.Data.Object is Session session)
            {
                sessionId = session.Id;
                session.Metadata?.TryGetValue("serverId", out serverId);
                session.Metadata?.TryGetValue("userId", out userId);
                amountTotal = session.AmountTotal ?? 0;
                subscriptionId = session.SubscriptionId;
            }
            else if (stripeEvent.Data.Object is Subscription sub)
            {
                subscriptionId = sub.Id;
                subscriptionStatus = sub.Status;
                // CurrentPeriodStart removed in Stripe SDK v48 — get from first subscription item
                var firstItem = sub.Items?.Data?.FirstOrDefault();
                if (firstItem?.CurrentPeriodStart != default)
                    periodStart = firstItem?.CurrentPeriodStart;
            }
            else if (stripeEvent.Data.Object is Invoice invoice)
            {
                subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
            }

            return new StripeWebhookEvent(stripeEvent.Type, sessionId, serverId, userId, amountTotal, subscriptionId, subscriptionStatus, periodStart);
        }
    }
}
