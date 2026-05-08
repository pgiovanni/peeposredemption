namespace peeposredemption.Domain.Entities;

public class PeepoSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? SubmitterName { get; set; }
    public string? Note { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
