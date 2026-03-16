namespace peeposredemption.Domain.Entities;

public class GameChannelConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public bool GameBotMuted { get; set; }
    public Guid? MutedByUserId { get; set; }
    public DateTime? MutedAt { get; set; }

    // Navigation
    public Channel Channel { get; set; } = null!;
}
