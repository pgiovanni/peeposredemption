namespace peeposredemption.Domain.Entities;

public class PlayerCharacter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public GameClass Class { get; set; } = GameClass.Warrior;
    public int Level { get; set; } = 1;
    public long XP { get; set; }

    // Base stats
    public int STR { get; set; } = 10;
    public int DEF { get; set; } = 10;
    public int INT { get; set; } = 10;
    public int DEX { get; set; } = 10;
    public int VIT { get; set; } = 10;
    public int LUK { get; set; } = 5;

    // HP / MP
    public int CurrentHp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int CurrentMp { get; set; } = 50;
    public int MaxMp { get; set; } = 50;

    // In-game currency (separate from Peepo Bucks/OrbBalance)
    public long CoinBalance { get; set; }

    // Tracking
    public int TotalMonstersKilled { get; set; }
    public int TotalDeaths { get; set; }

    // Chat integration
    public int ChatLevel { get; set; }

    // Cooldowns
    public DateTime? LastGatherAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<PlayerInventoryItem> Inventory { get; set; } = new List<PlayerInventoryItem>();
    public ICollection<PlayerSkill> Skills { get; set; } = new List<PlayerSkill>();
    public ICollection<CombatSession> CombatSessions { get; set; } = new List<CombatSession>();
}
