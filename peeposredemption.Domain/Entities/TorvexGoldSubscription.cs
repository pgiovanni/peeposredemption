namespace peeposredemption.Domain.Entities;

public enum SubscriptionStatus
{
    Pending = 0,
    Active = 1,
    Cancelled = 2,
    Expired = 3,
    PastDue = 4
}

public class TorvexGoldSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripeSessionId { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public DateTime? NextBillingAt { get; set; }
    // Tracks last renewal period start to prevent double-crediting orbs
    public DateTime? LastOrbCreditAt { get; set; }
}
