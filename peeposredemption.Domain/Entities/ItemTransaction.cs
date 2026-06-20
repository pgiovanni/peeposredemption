namespace peeposredemption.Domain.Entities;

public class ItemTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public PlayerCharacter Player { get; set; } = null!;
    public Guid ItemDefinitionId { get; set; }
    public ItemDefinition ItemDefinition { get; set; } = null!;
    public int Quantity { get; set; }  // negative = lost
    public CoinTransactionSource Source { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
