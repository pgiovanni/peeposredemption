namespace peeposredemption.Domain.Entities;

public class AltSuspicion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
    public int Score { get; set; }
    public string Signals { get; set; } = "[]"; // JSON array of signal strings
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public bool? IsConfirmed { get; set; } // null=pending, true=confirmed alt, false=dismissed

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
}
