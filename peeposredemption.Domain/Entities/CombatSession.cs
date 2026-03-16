namespace peeposredemption.Domain.Entities;

public class CombatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid MonsterDefinitionId { get; set; }
    public Guid ChannelId { get; set; }
    public CombatState State { get; set; } = CombatState.AwaitingAction;
    public int TurnNumber { get; set; } = 1;
    public bool IsPlayerTurn { get; set; } = true;

    // Monster state for this session
    public int MonsterCurrentHp { get; set; }
    public int MonsterMaxHp { get; set; }

    // Player snapshot
    public int PlayerHpAtStart { get; set; }
    public bool PlayerDefending { get; set; }

    // Log
    public string CombatLog { get; set; } = "[]";

    // Timestamps
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastTurnAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    // Navigation
    public PlayerCharacter Player { get; set; } = null!;
    public MonsterDefinition MonsterDefinition { get; set; } = null!;
}
