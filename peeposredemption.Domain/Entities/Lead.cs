namespace peeposredemption.Domain.Entities;

public enum LeadStatus { New, Contacted, Quoted, Won, Lost }

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Package { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
