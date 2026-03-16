using MediatR;
using peeposredemption.Domain.Entities;
using peeposredemption.Domain.Interfaces;

namespace peeposredemption.Application.Features.Game.Commands;

public record SeedGameDataCommand() : IRequest;

public class SeedGameDataCommandHandler : IRequestHandler<SeedGameDataCommand>
{
    private readonly IUnitOfWork _uow;
    public SeedGameDataCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(SeedGameDataCommand request, CancellationToken ct)
    {
        if (await _uow.ItemDefinitions.AnyAsync()) return; // Already seeded

        // ── Items ──

        // Weapons
        var woodenSword = Item("Wooden Sword", "A basic training sword.", GameItemType.Weapon, ItemSubType.Sword, EquipSlot.MainHand, GameItemRarity.Common, "⚔️", 1, 2, 5, buyPrice: 10, sellPrice: 3);
        var ironSword = Item("Iron Sword", "A sturdy iron blade.", GameItemType.Weapon, ItemSubType.Sword, EquipSlot.MainHand, GameItemRarity.Common, "⚔️", 5, 8, 14, bonusSTR: 2, buyPrice: 100, sellPrice: 30);
        var steelAxe = Item("Steel Axe", "A heavy steel axe.", GameItemType.Weapon, ItemSubType.Axe, EquipSlot.MainHand, GameItemRarity.Uncommon, "🪓", 10, 12, 20, bonusSTR: 4, buyPrice: 300, sellPrice: 90);
        var oakBow = Item("Oak Bow", "A reliable ranger's bow.", GameItemType.Weapon, ItemSubType.Bow, EquipSlot.MainHand, GameItemRarity.Common, "🏹", 5, 6, 12, bonusDEX: 3, buyPrice: 120, sellPrice: 35);
        var apprenticeStaff = Item("Apprentice Staff", "Channels magical energy.", GameItemType.Weapon, ItemSubType.Staff, EquipSlot.MainHand, GameItemRarity.Common, "🪄", 3, 4, 10, bonusINT: 3, buyPrice: 80, sellPrice: 25);
        var ironDagger = Item("Iron Dagger", "Quick and deadly.", GameItemType.Weapon, ItemSubType.Dagger, EquipSlot.MainHand, GameItemRarity.Common, "🗡️", 5, 5, 9, bonusDEX: 2, bonusLUK: 1, buyPrice: 90, sellPrice: 27);
        var steelSword = Item("Steel Sword", "A finely crafted steel blade.", GameItemType.Weapon, ItemSubType.Sword, EquipSlot.MainHand, GameItemRarity.Uncommon, "⚔️", 15, 15, 25, bonusSTR: 5, buyPrice: 500, sellPrice: 150);
        var fireBlade = Item("Fire Blade", "Burns with elemental fury.", GameItemType.Weapon, ItemSubType.Sword, EquipSlot.MainHand, GameItemRarity.Rare, "🔥", 25, 20, 35, bonusSTR: 6, element: Element.Fire, buyPrice: 1500, sellPrice: 500);
        var frostBow = Item("Frost Bow", "Arrows freeze on impact.", GameItemType.Weapon, ItemSubType.Bow, EquipSlot.MainHand, GameItemRarity.Rare, "❄️", 25, 18, 30, bonusDEX: 6, element: Element.Ice, buyPrice: 1500, sellPrice: 500);
        var thunderStaff = Item("Thunder Staff", "Crackling with lightning.", GameItemType.Weapon, ItemSubType.Staff, EquipSlot.MainHand, GameItemRarity.Rare, "⚡", 30, 25, 40, bonusINT: 8, element: Element.Lightning, buyPrice: 2000, sellPrice: 700);

        // Armor
        var leatherHelmet = Item("Leather Helmet", "Basic head protection.", GameItemType.Armor, ItemSubType.Helmet, EquipSlot.Head, GameItemRarity.Common, "🪖", 1, bonusDEF: 2, buyPrice: 30, sellPrice: 10);
        var leatherChest = Item("Leather Chestplate", "Light and flexible.", GameItemType.Armor, ItemSubType.Chestplate, EquipSlot.Chest, GameItemRarity.Common, "🦺", 1, bonusDEF: 4, buyPrice: 60, sellPrice: 18);
        var leatherLegs = Item("Leather Leggings", "Flexible leg protection.", GameItemType.Armor, ItemSubType.Leggings, EquipSlot.Legs, GameItemRarity.Common, "👖", 1, bonusDEF: 3, buyPrice: 45, sellPrice: 14);
        var leatherBoots = Item("Leather Boots", "Light footwear.", GameItemType.Armor, ItemSubType.Boots, EquipSlot.Feet, GameItemRarity.Common, "👢", 1, bonusDEF: 1, bonusDEX: 1, buyPrice: 25, sellPrice: 8);
        var ironHelmet = Item("Iron Helmet", "Sturdy iron headpiece.", GameItemType.Armor, ItemSubType.Helmet, EquipSlot.Head, GameItemRarity.Uncommon, "⛑️", 10, bonusDEF: 5, buyPrice: 200, sellPrice: 60);
        var ironChest = Item("Iron Chestplate", "Heavy iron armor.", GameItemType.Armor, ItemSubType.Chestplate, EquipSlot.Chest, GameItemRarity.Uncommon, "🛡️", 10, bonusDEF: 8, buyPrice: 400, sellPrice: 120);
        var ironLegs = Item("Iron Leggings", "Solid leg armor.", GameItemType.Armor, ItemSubType.Leggings, EquipSlot.Legs, GameItemRarity.Uncommon, "🦿", 10, bonusDEF: 6, buyPrice: 300, sellPrice: 90);
        var ironBoots = Item("Iron Boots", "Heavy iron boots.", GameItemType.Armor, ItemSubType.Boots, EquipSlot.Feet, GameItemRarity.Uncommon, "🥾", 10, bonusDEF: 3, buyPrice: 150, sellPrice: 45);
        var woodenShield = Item("Wooden Shield", "A simple wooden shield.", GameItemType.Armor, ItemSubType.Shield, EquipSlot.OffHand, GameItemRarity.Common, "🛡️", 1, bonusDEF: 3, buyPrice: 40, sellPrice: 12);

        // Consumables
        var smallHpPotion = Item("Small Health Potion", "Restores 25 HP.", GameItemType.Consumable, ItemSubType.HealthPotion, null, GameItemRarity.Common, "🧪", 1, healAmount: 25, stackable: true, buyPrice: 15, sellPrice: 5);
        var hpPotion = Item("Health Potion", "Restores 50 HP.", GameItemType.Consumable, ItemSubType.HealthPotion, null, GameItemRarity.Uncommon, "🧪", 5, healAmount: 50, stackable: true, buyPrice: 40, sellPrice: 12);
        var smallMpPotion = Item("Small Mana Potion", "Restores 20 MP.", GameItemType.Consumable, ItemSubType.ManaPotion, null, GameItemRarity.Common, "💧", 1, manaRestore: 20, stackable: true, buyPrice: 15, sellPrice: 5);
        var mpPotion = Item("Mana Potion", "Restores 40 MP.", GameItemType.Consumable, ItemSubType.ManaPotion, null, GameItemRarity.Uncommon, "💧", 5, manaRestore: 40, stackable: true, buyPrice: 40, sellPrice: 12);

        // Materials
        var ironOre = Item("Iron Ore", "Raw iron ore.", GameItemType.Material, ItemSubType.Ore, null, GameItemRarity.Common, "🪨", 1, stackable: true, buyPrice: 5, sellPrice: 2);
        var copperOre = Item("Copper Ore", "Raw copper ore.", GameItemType.Material, ItemSubType.Ore, null, GameItemRarity.Common, "🟤", 1, stackable: true, buyPrice: 3, sellPrice: 1);
        var wood = Item("Wood", "A sturdy log.", GameItemType.Material, ItemSubType.Wood, null, GameItemRarity.Common, "🪵", 1, stackable: true, buyPrice: 3, sellPrice: 1);
        var leather = Item("Leather", "Tanned animal hide.", GameItemType.Material, ItemSubType.Leather, null, GameItemRarity.Common, "🟫", 1, stackable: true, buyPrice: 8, sellPrice: 3);
        var goblinFang = Item("Goblin Fang", "A trophy from a goblin.", GameItemType.Material, ItemSubType.MonsterDrop, null, GameItemRarity.Common, "🦷", 1, stackable: true, buyPrice: 10, sellPrice: 4);
        var spiderSilk = Item("Spider Silk", "Strong and flexible thread.", GameItemType.Material, ItemSubType.MonsterDrop, null, GameItemRarity.Uncommon, "🕸️", 1, stackable: true, buyPrice: 20, sellPrice: 8);
        var wolfPelt = Item("Wolf Pelt", "A thick wolf hide.", GameItemType.Material, ItemSubType.MonsterDrop, null, GameItemRarity.Common, "🐺", 1, stackable: true, buyPrice: 12, sellPrice: 5);

        var allItems = new[] { woodenSword, ironSword, steelAxe, oakBow, apprenticeStaff, ironDagger, steelSword, fireBlade, frostBow, thunderStaff,
            leatherHelmet, leatherChest, leatherLegs, leatherBoots, ironHelmet, ironChest, ironLegs, ironBoots, woodenShield,
            smallHpPotion, hpPotion, smallMpPotion, mpPotion,
            ironOre, copperOre, wood, leather, goblinFang, spiderSilk, wolfPelt };

        foreach (var item in allItems)
            await _uow.ItemDefinitions.AddAsync(item);

        // ── Monsters ──

        var monsters = new (MonsterDefinition monster, (ItemDefinition item, decimal chance, int minQty, int maxQty)[] loot)[]
        {
            (Monster("Slime", "A wobbly green blob.", "🟢", 1, "Plains", 30, 3, 2, 2, 1, 1, 4, Element.None, 15, 2, 5),
                new[] { (smallHpPotion, 0.3m, 1, 1) }),
            (Monster("Goblin", "A sneaky little troublemaker.", "👺", 3, "Plains", 50, 6, 3, 3, 4, 3, 8, Element.None, 30, 5, 12),
                new[] { (goblinFang, 0.5m, 1, 2), (copperOre, 0.2m, 1, 1) }),
            (Monster("Wolf", "A fierce grey wolf.", "🐺", 5, "Plains", 70, 8, 4, 3, 6, 5, 10, Element.None, 45, 8, 18),
                new[] { (wolfPelt, 0.6m, 1, 1), (leather, 0.3m, 1, 1) }),

            (Monster("Giant Spider", "Webs everything in sight.", "🕷️", 7, "Forest", 100, 10, 5, 4, 7, 7, 14, Element.None, 70, 12, 25),
                new[] { (spiderSilk, 0.5m, 1, 2), (smallHpPotion, 0.2m, 1, 1) }),
            (Monster("Orc", "A hulking green brute.", "👹", 9, "Forest", 140, 14, 8, 5, 5, 10, 18, Element.None, 100, 18, 35),
                new[] { (ironOre, 0.4m, 1, 2), (ironSword, 0.05m, 1, 1) }),
            (Monster("Forest Troll", "Regenerates in the shade.", "🧌", 10, "Forest", 180, 12, 10, 4, 3, 12, 20, Element.Earth, 120, 22, 40),
                new[] { (wood, 0.5m, 1, 3), (hpPotion, 0.15m, 1, 1) }),

            (Monster("Rock Golem", "Made of living stone.", "🗿", 14, "Mountains", 250, 16, 15, 6, 3, 15, 25, Element.Earth, 180, 30, 55),
                new[] { (ironOre, 0.6m, 2, 4), (copperOre, 0.4m, 1, 3) }),
            (Monster("Harpy", "Attacks from the sky.", "🦅", 16, "Mountains", 200, 14, 8, 10, 12, 12, 22, Element.Lightning, 210, 35, 60),
                new[] { (smallMpPotion, 0.3m, 1, 1), (oakBow, 0.05m, 1, 1) }),
            (Monster("Mountain Ogre", "Shakes the ground when walking.", "👾", 19, "Mountains", 320, 20, 14, 5, 4, 18, 30, Element.None, 280, 45, 80),
                new[] { (steelAxe, 0.03m, 1, 1), (hpPotion, 0.25m, 1, 1) }),

            (Monster("Skeleton Knight", "A cursed warrior risen.", "💀", 24, "Dungeon", 380, 22, 18, 8, 8, 20, 32, Element.Dark, 400, 55, 100),
                new[] { (ironHelmet, 0.05m, 1, 1), (ironSword, 0.08m, 1, 1) }),
            (Monster("Dark Mage", "Casts forbidden spells.", "🧙", 28, "Dungeon", 300, 12, 10, 22, 10, 18, 35, Element.Dark, 500, 70, 120),
                new[] { (apprenticeStaff, 0.08m, 1, 1), (mpPotion, 0.3m, 1, 1) }),
            (Monster("Minotaur", "Guards the labyrinth.", "🐂", 33, "Dungeon", 500, 28, 22, 8, 6, 25, 40, Element.None, 650, 90, 160),
                new[] { (steelSword, 0.05m, 1, 1), (hpPotion, 0.3m, 1, 2) }),

            (Monster("Fire Elemental", "Pure living flame.", "🔥", 38, "Volcano", 420, 24, 15, 20, 12, 28, 45, Element.Fire, 800, 120, 200),
                new[] { (fireBlade, 0.02m, 1, 1), (ironOre, 0.5m, 2, 5) }),
            (Monster("Lava Wyrm", "A serpent of molten rock.", "🐉", 43, "Volcano", 600, 30, 25, 15, 10, 35, 55, Element.Fire, 1000, 150, 250),
                new[] { (hpPotion, 0.4m, 1, 2), (mpPotion, 0.3m, 1, 1) }),
            (Monster("Inferno Drake", "Breathes rivers of fire.", "🐲", 48, "Volcano", 750, 35, 28, 18, 14, 40, 60, Element.Fire, 1300, 200, 350),
                new[] { (fireBlade, 0.05m, 1, 1), (thunderStaff, 0.02m, 1, 1) }),

            (Monster("Shadow Demon", "Born from the abyss.", "👿", 55, "Abyss", 900, 40, 30, 30, 20, 45, 70, Element.Dark, 1800, 300, 500),
                new[] { (thunderStaff, 0.03m, 1, 1), (frostBow, 0.03m, 1, 1) }),
            (Monster("Ancient Dragon", "The ultimate challenge.", "🐉", 70, "Abyss", 1500, 55, 45, 40, 25, 60, 100, Element.Fire, 5000, 500, 1000),
                new[] { (fireBlade, 0.1m, 1, 1), (frostBow, 0.1m, 1, 1), (thunderStaff, 0.1m, 1, 1) }),
        };

        foreach (var (monster, loot) in monsters)
        {
            await _uow.MonsterDefinitions.AddAsync(monster);
            foreach (var (item, chance, minQ, maxQ) in loot)
            {
                await _uow.MonsterLootEntries.AddAsync(new MonsterLootEntry
                {
                    MonsterDefinitionId = monster.Id,
                    ItemDefinitionId = item.Id,
                    DropChance = chance,
                    MinQuantity = minQ,
                    MaxQuantity = maxQ
                });
            }
        }

        await _uow.SaveChangesAsync();
    }

    private static ItemDefinition Item(string name, string desc, GameItemType type, ItemSubType subType,
        EquipSlot? equipSlot, GameItemRarity rarity, string icon, int levelReq,
        int minDmg = 0, int maxDmg = 0,
        int bonusSTR = 0, int bonusDEF = 0, int bonusINT = 0, int bonusDEX = 0, int bonusVIT = 0, int bonusLUK = 0,
        int healAmount = 0, int manaRestore = 0, bool stackable = false,
        long buyPrice = 0, long sellPrice = 0, Element element = Element.None)
    {
        return new ItemDefinition
        {
            Name = name, Description = desc, Type = type, SubType = subType,
            EquipSlot = equipSlot, Rarity = rarity, Icon = icon, LevelReq = levelReq,
            MinDamage = minDmg, MaxDamage = maxDmg,
            BonusSTR = bonusSTR, BonusDEF = bonusDEF, BonusINT = bonusINT,
            BonusDEX = bonusDEX, BonusVIT = bonusVIT, BonusLUK = bonusLUK,
            HealAmount = healAmount, ManaRestoreAmount = manaRestore,
            IsStackable = stackable, BuyPrice = buyPrice, SellPrice = sellPrice,
            Element = element
        };
    }

    private static MonsterDefinition Monster(string name, string desc, string icon, int level, string zone,
        int maxHp, int str, int def, int @int, int dex, int minDmg, int maxDmg,
        Element element, long xpReward, long orbMin, long orbMax)
    {
        return new MonsterDefinition
        {
            Name = name, Description = desc, Icon = icon, Level = level, Zone = zone,
            MaxHp = maxHp, STR = str, DEF = def, INT = @int, DEX = dex,
            MinDamage = minDmg, MaxDamage = maxDmg, Element = element,
            XpReward = xpReward, OrbRewardMin = orbMin, OrbRewardMax = orbMax
        };
    }
}
