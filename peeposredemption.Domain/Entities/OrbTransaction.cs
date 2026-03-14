namespace peeposredemption.Domain.Entities;

public enum OrbTransactionType
{
    DailyLogin = 0,
    MessageReward = 1,
    StripePurchase = 2,
    GiftSent = 3,
    GiftReceived = 4,
    CrateOpen = 5,
    TradeSpent = 6,
    TradeReceived = 7,
    StockBuy = 8,
    StockSell = 9,
    AdminGrant = 10,
    CraftingSpent = 11,
    MarketplaceSale = 12,
    MarketplacePurchase = 13
}

public class OrbTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public long Amount { get; set; }
    public OrbTransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? RelatedUserId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
