namespace peeposredemption.Domain.Entities;

public class UserFingerprint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FingerprintHash { get; set; } = null!;
    public string? RawComponents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
