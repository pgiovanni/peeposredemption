namespace peeposredemption.Domain.Entities;

public enum FeatureRequestStatus { Pending, Reviewed, Planned, Done, Dismissed }

public class FeatureRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeatureRequestStatus Status { get; set; } = FeatureRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
