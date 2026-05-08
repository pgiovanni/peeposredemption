namespace peeposredemption.Domain.Entities;

public class PeepoRarityVote
{
    public string PeepoName { get; set; } = string.Empty;   // composite PK
    public string IpAddress { get; set; } = string.Empty;   // composite PK
    public string VotedRarity { get; set; } = string.Empty; // "Common"|"Uncommon"|"Rare"|"Epic"|"Legendary"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
