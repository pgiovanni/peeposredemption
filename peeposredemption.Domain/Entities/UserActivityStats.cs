namespace peeposredemption.Domain.Entities;

public class UserActivityStats
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public long TotalMessages { get; set; }
    public int LongestStreak { get; set; }
    public long TotalOrbsGifted { get; set; }
    public int ServersJoined { get; set; }
    public long PeakOrbBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
