namespace peeposredemption.Domain.Entities;

public enum SupportTicketCategory { BugReport, AccountHelp, GeneralQuestion, TrustSafety }
public enum SupportTicketStatus { Open, InProgress, Resolved, Closed }

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public SupportTicketCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReportedMessageId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public User User { get; set; } = null!;
}
