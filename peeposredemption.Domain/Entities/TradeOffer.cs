namespace peeposredemption.Domain.Entities;

public class TradeOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InitiatorId { get; set; }
    public Guid RecipientId { get; set; }
    public Guid ChannelId { get; set; }
    public TradeStatus Status { get; set; } = TradeStatus.Pending;

    // Items as JSON arrays of { ItemDefinitionId, Quantity }
    public string InitiatorItems { get; set; } = "[]";
    public long InitiatorOrbs { get; set; }
    public string RecipientItems { get; set; } = "[]";
    public long RecipientOrbs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);

    // Navigation
    public PlayerCharacter Initiator { get; set; } = null!;
    public PlayerCharacter Recipient { get; set; } = null!;
}
