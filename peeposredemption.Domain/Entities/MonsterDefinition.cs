namespace peeposredemption.Domain.Entities;

public class MonsterDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Zone { get; set; } = string.Empty;
    public int MaxHp { get; set; }

    // Stats
    public int STR { get; set; }
    public int DEF { get; set; }
    public int INT { get; set; }
    public int DEX { get; set; }

    // Damage
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public Element Element { get; set; } = Element.None;

    // Rewards
    public long XpReward { get; set; }
    public long OrbRewardMin { get; set; }
    public long OrbRewardMax { get; set; }

    // Navigation
    public ICollection<MonsterLootEntry> LootTable { get; set; } = new List<MonsterLootEntry>();
}
