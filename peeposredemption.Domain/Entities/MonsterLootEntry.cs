namespace peeposredemption.Domain.Entities;

public class MonsterLootEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonsterDefinitionId { get; set; }
    public Guid ItemDefinitionId { get; set; }
    public decimal DropChance { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;

    // Navigation
    public MonsterDefinition MonsterDefinition { get; set; } = null!;
    public ItemDefinition ItemDefinition { get; set; } = null!;
}
