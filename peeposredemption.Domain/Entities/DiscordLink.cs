namespace peeposredemption.Domain.Entities;

public class DiscordLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DiscordUserId { get; set; } = string.Empty;
    public Guid TorvexUserId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
