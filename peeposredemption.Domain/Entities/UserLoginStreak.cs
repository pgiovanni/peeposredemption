namespace peeposredemption.Domain.Entities;

public class UserLoginStreak
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastClaimedDate { get; set; }
    public int MessageCountToday { get; set; }
    public DateTime? MessageCountDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
