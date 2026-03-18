namespace peeposredemption.Domain.Entities;

public class UserIpLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = null!;
    public bool IsVpn { get; set; }
    public bool IsTor { get; set; }
    public DateTime SeenAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
