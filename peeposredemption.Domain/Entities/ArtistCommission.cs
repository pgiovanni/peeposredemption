namespace peeposredemption.Domain.Entities;

public enum CommissionSource
{
    CrateDrop = 0,
    MarketplaceSale = 1
}

public class ArtistCommission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public Guid ArtItemId { get; set; }
    public ArtItem ArtItem { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public long OrbAmount { get; set; }
    public long CommissionOrbs { get; set; }
    public long CommissionCents { get; set; }
    public CommissionSource Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
