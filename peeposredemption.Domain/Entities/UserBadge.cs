namespace peeposredemption.Domain.Entities;

public class UserBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BadgeDefinitionId { get; set; }
    public BadgeDefinition BadgeDefinition { get; set; } = null!;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public bool IsDisplayed { get; set; }
}
