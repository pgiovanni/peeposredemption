namespace peeposredemption.Domain.Entities;

public class GuildConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GuildId { get; set; } = string.Empty; // Discord guild snowflake ID

    public string? StatusChannelId { get; set; }      // Bot online/offline notices
    public string? LootDropChannelId { get; set; }    // Crate opens, rare drops, boss kills
    public string? RpgChannelId { get; set; }         // Fight results
    public string? SuggestionsChannelId { get; set; } // /suggest posts
    public string? WelcomeChannelId { get; set; }     // New member greetings
    public string? ModLogChannelId { get; set; }      // Mod actions

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
