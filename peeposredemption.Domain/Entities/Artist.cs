namespace peeposredemption.Domain.Entities;

public enum PayoutMethod
{
    PayPal = 0,
    Venmo = 1
}

public class Artist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string PayoutEmail { get; set; } = string.Empty;
    public PayoutMethod PayoutMethod { get; set; }
    public long TotalEarnedCents { get; set; }
    public long TotalPaidCents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
