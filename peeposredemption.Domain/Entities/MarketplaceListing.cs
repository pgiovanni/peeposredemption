namespace peeposredemption.Domain.Entities;

public class MarketplaceListing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public Guid ItemDefinitionId { get; set; }
    public int Quantity { get; set; }
    public long PricePerUnit { get; set; }
    public MarketListingStatus Status { get; set; } = MarketListingStatus.Active;
    public Guid? BuyerId { get; set; }
    public MarketplaceCurrencyType CurrencyType { get; set; } = MarketplaceCurrencyType.Orbs;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    // Navigation
    public PlayerCharacter Seller { get; set; } = null!;
    public PlayerCharacter? Buyer { get; set; }
    public ItemDefinition ItemDefinition { get; set; } = null!;
}
