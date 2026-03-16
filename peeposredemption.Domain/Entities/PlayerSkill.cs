namespace peeposredemption.Domain.Entities;

public class PlayerSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public SkillType SkillType { get; set; }
    public int Level { get; set; } = 1;
    public long XP { get; set; }
    public long XpToNextLevel { get; set; } = 75;

    // Navigation
    public PlayerCharacter Player { get; set; } = null!;
}
