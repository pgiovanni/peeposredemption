namespace peeposredemption.Domain.Entities;

public enum BadgeCategory
{
    Activity = 0,
    Social = 1,
    Economy = 2
}

public class BadgeDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // emoji or icon class
    public BadgeCategory Category { get; set; }
    public string StatKey { get; set; } = string.Empty; // which UserActivityStats field to check
    public long Threshold { get; set; } // value needed to earn this badge
    public long OrbReward { get; set; } // orbs granted when badge is earned
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
