namespace peeposredemption.Domain.Entities;

public class OrbGift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public Guid RecipientId { get; set; }
    public User Recipient { get; set; } = null!;
    public long Amount { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ServerId { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
