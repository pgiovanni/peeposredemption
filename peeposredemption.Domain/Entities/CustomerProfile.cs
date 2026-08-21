namespace peeposredemption.Domain.Entities;

// Contact + billing details for the customer side of an account (IT-solutions
// clients). One-to-one with User; the chat identity stays on User itself.
public class CustomerProfile
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
