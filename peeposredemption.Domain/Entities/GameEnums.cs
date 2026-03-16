namespace peeposredemption.Domain.Entities;

public enum GameClass
{
    Warrior,
    Mage,
    Ranger,
    Cleric,
    Rogue
}

public enum GameItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    QuestItem
}

public enum ItemSubType
{
    // Weapons
    Sword, Axe, Bow, Staff, Dagger,
    // Armor
    Helmet, Chestplate, Leggings, Boots, Shield, Ring, Amulet,
    // Consumables
    HealthPotion, ManaPotion,
    // Materials
    Ore, Herb, Wood, Gem, MonsterDrop, Leather, Cloth
}

public enum EquipSlot
{
    MainHand,
    OffHand,
    Head,
    Chest,
    Legs,
    Feet,
    Ring,
    Amulet
}

public enum GameItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum Element
{
    None,
    Fire,
    Ice,
    Lightning,
    Earth,
    Dark,
    Holy
}

public enum CombatState
{
    AwaitingAction,
    InProgress,
    Victory,
    Defeat,
    Fled,
    Expired
}

public enum CombatAction
{
    Attack,
    Defend,
    Magic,
    UseItem,
    Flee
}

public enum TradeStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled,
    Expired
}

public enum MarketListingStatus
{
    Active,
    Sold,
    Cancelled,
    Expired
}

public enum SkillType
{
    Combat,
    Mining,
    Smithing,
    Woodcutting,
    Alchemy,
    Fishing,
    Cooking,
    Enchanting
}
