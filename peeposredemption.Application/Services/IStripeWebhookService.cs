namespace peeposredemption.Application.Services
{
    public record StripeWebhookEvent(
        string Type,
        string? SessionId,
        string? ServerId,
        string? UserId = null,
        long AmountTotal = 0,
        string? SubscriptionId = null,
        string? SubscriptionStatus = null,
        DateTime? PeriodStart = null,
        string? PaymentIntentId = null,
        string? InvoiceId = null,
        string? CustomerId = null);

    public interface IStripeWebhookService
    {
        StripeWebhookEvent ParseAndVerify(string payload, string signature);
    }
}
