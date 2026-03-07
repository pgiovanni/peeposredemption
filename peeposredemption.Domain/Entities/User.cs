namespace peeposredemption.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool EmailConfirmed {  get; set; }
    public string? EmailConfirmationtoken { get; set; }
 
    public ICollection<ServerMember> ServerMemberships { get; set; }
    public ICollection<Message> Messages { get; set; }
    public ICollection<DirectMessage> SentDirectMessages { get; set; }
    public ICollection<DirectMessage> ReceivedDirectMessages { get; set; }
}
