namespace peeposredemption.Domain.Entities;

public enum OrbPackTier
{
    Pack100 = 0,
    Pack600 = 1,
    Pack1500 = 2
}

public class OrbPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string StripeSessionId { get; set; } = string.Empty;
    public int OrbAmount { get; set; }
    public long PriceCents { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
