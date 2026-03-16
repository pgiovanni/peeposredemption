namespace peeposredemption.Domain.Entities;

public enum SubmissionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class ArtistSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string SampleImageUrls { get; set; } = "[]";
    public string SampleImageKeys { get; set; } = "[]";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
}
