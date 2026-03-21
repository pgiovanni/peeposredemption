namespace peeposredemption.Domain.Entities;

public class VoiceSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid ServerId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime LeftAt { get; set; }
    public long OrbsEarned { get; set; }

    public User User { get; set; } = null!;
}
