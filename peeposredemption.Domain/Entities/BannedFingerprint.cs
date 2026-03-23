namespace peeposredemption.Domain.Entities;

public class BannedFingerprint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FingerprintHash { get; set; } = string.Empty;
    public Guid BannedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User BannedBy { get; set; } = null!;
}
