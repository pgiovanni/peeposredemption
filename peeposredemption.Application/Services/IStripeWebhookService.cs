namespace peeposredemption.Application.Services
{
    public record StripeWebhookEvent(string Type, string? SessionId, string? ServerId, string? UserId = null, long AmountTotal = 0);

    public interface IStripeWebhookService
    {
        StripeWebhookEvent ParseAndVerify(string payload, string signature);
    }
}
