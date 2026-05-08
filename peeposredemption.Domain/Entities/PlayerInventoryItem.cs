using System.Text.Json;

namespace peeposredemption.Domain.Entities;

public class PlayerInventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid ItemDefinitionId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsEquipped { get; set; }
    public EquipSlot? EquippedSlot { get; set; }

    // Enchantments — JSON array of EnchantmentSlot records
    public string? EnchantsJson { get; set; }

    // Navigation
    public PlayerCharacter Player { get; set; } = null!;
    public ItemDefinition ItemDefinition { get; set; } = null!;

    // ── Enchant helpers ────────────────────────────────────────────────────────

    public List<EnchantmentSlot> GetEnchants()
    {
        if (string.IsNullOrWhiteSpace(EnchantsJson)) return new();
        try { return JsonSerializer.Deserialize<List<EnchantmentSlot>>(EnchantsJson) ?? new(); }
        catch { return new(); }
    }

    public void SetEnchants(List<EnchantmentSlot> enchants)
        => EnchantsJson = enchants.Count > 0 ? JsonSerializer.Serialize(enchants) : null;

    /// <summary>Returns the primary (first) enchant element, or Element.None if unenchanted.</summary>
    public Element GetPrimaryElement()
        => GetEnchants().FirstOrDefault()?.Element ?? Element.None;

    /// <summary>Max enchant slots based on item rarity (Common=1 … Legendary=5).</summary>
    public static int MaxSlotsForRarity(GameItemRarity rarity) => rarity switch
    {
        GameItemRarity.Common    => 1,
        GameItemRarity.Uncommon  => 2,
        GameItemRarity.Rare      => 3,
        GameItemRarity.Epic      => 4,
        GameItemRarity.Legendary => 5,
        _ => 1
    };
}
