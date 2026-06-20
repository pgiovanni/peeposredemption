namespace peeposredemption.Domain.Entities;

public class CoinTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public PlayerCharacter Player { get; set; } = null!;
    public long Amount { get; set; }  // negative = spent
    public CoinTransactionSource Source { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
