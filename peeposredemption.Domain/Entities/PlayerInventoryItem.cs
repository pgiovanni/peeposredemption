namespace peeposredemption.Domain.Entities;

public class PlayerInventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid ItemDefinitionId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsEquipped { get; set; }
    public EquipSlot? EquippedSlot { get; set; }

    // Enchantment
    public Element? EnchantElement { get; set; }
    public int EnchantBonus { get; set; }
    public string? EnchantName { get; set; }

    // Navigation
    public PlayerCharacter Player { get; set; } = null!;
    public ItemDefinition ItemDefinition { get; set; } = null!;
}
