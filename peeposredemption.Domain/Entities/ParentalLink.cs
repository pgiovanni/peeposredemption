namespace peeposredemption.Domain.Entities;

public enum ParentalLinkStatus
{
    Pending,
    Active,
    Revoked
}

public class ParentalLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentUserId { get; set; }
    public Guid ChildUserId { get; set; }
    public string LinkCode { get; set; }
    public ParentalLinkStatus Status { get; set; } = ParentalLinkStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool AccountFrozen { get; set; }
    public bool DmFriendsOnly { get; set; } = true;

    public User? Parent { get; set; }
    public User Child { get; set; }
}
