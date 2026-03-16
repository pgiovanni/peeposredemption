namespace peeposredemption.Domain.Entities;

public class ItemDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GameItemType Type { get; set; }
    public ItemSubType SubType { get; set; }
    public EquipSlot? EquipSlot { get; set; }
    public GameItemRarity Rarity { get; set; } = GameItemRarity.Common;
    public string Icon { get; set; } = string.Empty;
    public int LevelReq { get; set; }
    public GameClass? ClassReq { get; set; }
    public bool IsStackable { get; set; }
    public long BuyPrice { get; set; }
    public long SellPrice { get; set; }

    // Stat bonuses
    public int BonusSTR { get; set; }
    public int BonusDEF { get; set; }
    public int BonusINT { get; set; }
    public int BonusDEX { get; set; }
    public int BonusVIT { get; set; }
    public int BonusLUK { get; set; }
    public int BonusHP { get; set; }
    public int BonusMP { get; set; }

    // Weapon stats
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public Element Element { get; set; } = Element.None;

    // Consumable stats
    public int HealAmount { get; set; }
    public int ManaRestoreAmount { get; set; }
}
