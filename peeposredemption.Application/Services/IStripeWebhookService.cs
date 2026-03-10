namespace peeposredemption.Application.Services
{
    public record StripeWebhookEvent(string Type, string? SessionId, string? ServerId, long AmountTotal = 0);

    public interface IStripeWebhookService
    {
        StripeWebhookEvent ParseAndVerify(string payload, string signature);
    }
}
