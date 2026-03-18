namespace peeposredemption.Domain.Entities;

public class ReferralCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Code { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string? Label { get; set; }
    public int LinkCopies { get; set; }
    public int LinkClicks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; }
    public ICollection<ReferralPurchase> Purchases { get; set; }
}

public class ReferralPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReferralCodeId { get; set; }
    public Guid PurchaserId { get; set; }
    public long AmountCents { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReferralCode ReferralCode { get; set; }
    public User Purchaser { get; set; }
}
