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
    QuestItem,
    Collectible = 5
}

public enum MarketplaceCurrencyType
{
    Orbs,
    Coins
}

public enum ItemSubType
{
    // Weapons
    Sword       = 0,
    Axe         = 1,
    Bow         = 2,
    Staff       = 3,
    Dagger      = 4,
    Gun         = 5,
    // Armor
    Helmet      = 6,
    Chestplate  = 7,
    Leggings    = 8,
    Boots       = 9,
    Shield      = 10,
    Ring        = 11,
    Amulet      = 12,
    // Consumables
    HealthPotion = 13,
    ManaPotion   = 14,
    // Materials
    Ore         = 15,
    Herb        = 16,
    Wood        = 17,
    Gem         = 18,
    MonsterDrop = 19,
    Leather     = 20,
    Cloth       = 21,
    // Collectibles
    Peepo       = 22,
    // Enchanting
    EnchantBook = 23,
    // Gathering tools
    Pickaxe     = 24,
    FishingRod  = 25
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
    Water,
    Wind,
    Dark,
    Holy,
    Light,
    Shadow,
    Poison,
    Void
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

public enum CoinTransactionSource
{
    Combat       = 0,
    Shop         = 1,
    Trade        = 2,
    Marketplace  = 3,
    AdminGrant   = 4,
    CrateOpen    = 5,
    Other        = 6
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
