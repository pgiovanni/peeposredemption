namespace peeposredemption.Domain.Entities;

public class ArtistPayout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public long AmountCents { get; set; }
    public PayoutMethod PayoutMethod { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
}
