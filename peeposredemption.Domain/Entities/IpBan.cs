namespace peeposredemption.Domain.Entities;

public class IpBan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IpAddress { get; set; } = null!;
    public string? Reason { get; set; }
    public Guid BannedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public User BannedBy { get; set; } = null!;
}
