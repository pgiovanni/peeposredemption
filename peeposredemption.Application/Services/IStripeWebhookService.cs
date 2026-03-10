namespace peeposredemption.Application.Services
{
    public record StripeWebhookEvent(string Type, string? SessionId, string? ServerId);

    public interface IStripeWebhookService
    {
        StripeWebhookEvent ParseAndVerify(string payload, string signature);
    }
}
