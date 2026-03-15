namespace peeposredemption.Domain.Entities;

public enum ItemRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

public enum ItemType
{
    ProfileBorder = 0,
    ProfileBanner = 1,
    BadgeIcon = 2,
    AvatarBackground = 3
}

public class ArtItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemRarity Rarity { get; set; }
    public ItemType ItemType { get; set; }
    public string AssetUrl { get; set; } = string.Empty;
    public string R2Key { get; set; } = string.Empty;
    public long OrbValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
