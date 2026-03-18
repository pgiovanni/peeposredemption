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

            if (stripeEvent.Data.Object is Session session)
            {
                sessionId = session.Id;
                session.Metadata?.TryGetValue("serverId", out serverId);
                session.Metadata?.TryGetValue("userId", out userId);
                amountTotal = session.AmountTotal ?? 0;
            }

            return new StripeWebhookEvent(stripeEvent.Type, sessionId, serverId, userId, amountTotal);
        }
    }
}
