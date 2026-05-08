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
        await PatchMonsterAbilitiesAsync();
        await PatchConsumablesAsync();

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
            (Monster("Slime", "A wobbly green blob.", "🟢", 1, "Plains", 30, 3, 2, 2, 1, 1, 4, Element.None, 15, 2, 5,
                SA(("Slow", 0.20f, 0, 2))),
                new[] { (smallHpPotion, 0.3m, 1, 1) }),
            (Monster("Goblin", "A sneaky little troublemaker.", "👺", 3, "Plains", 50, 6, 3, 3, 4, 3, 8, Element.None, 30, 5, 12,
                SA(("Bleed", 0.30f, 4, 2))),
                new[] { (goblinFang, 0.5m, 1, 2), (copperOre, 0.2m, 1, 1) }),
            (Monster("Wolf", "A fierce grey wolf.", "🐺", 5, "Plains", 70, 8, 4, 3, 6, 5, 10, Element.None, 45, 8, 18,
                SA(("Bleed", 0.40f, 6, 3))),
                new[] { (wolfPelt, 0.6m, 1, 1), (leather, 0.3m, 1, 1) }),

            (Monster("Giant Spider", "Webs everything in sight.", "🕷️", 7, "Forest", 100, 10, 5, 4, 7, 7, 14, Element.None, 70, 12, 25,
                SA(("Poison", 0.50f, 3, 3))),
                new[] { (spiderSilk, 0.5m, 1, 2), (smallHpPotion, 0.2m, 1, 1) }),
            (Monster("Orc", "A hulking green brute.", "👹", 9, "Forest", 140, 14, 8, 5, 5, 10, 18, Element.None, 100, 18, 35,
                SA(("AttackDown", 0.30f, 4, 2))),
                new[] { (ironOre, 0.4m, 1, 2), (ironSword, 0.05m, 1, 1) }),
            (Monster("Forest Troll", "Regenerates in the shade.", "🧌", 10, "Forest", 180, 12, 10, 4, 3, 12, 20, Element.Earth, 120, 22, 40,
                SA(("Bleed", 0.40f, 8, 3), ("Slow", 0.25f, 0, 2))),
                new[] { (wood, 0.5m, 1, 3), (hpPotion, 0.15m, 1, 1) }),

            (Monster("Rock Golem", "Made of living stone.", "🗿", 14, "Mountains", 250, 16, 15, 6, 3, 15, 25, Element.Earth, 180, 30, 55,
                SA(("DefenseDown", 0.35f, 5, 3), ("Slow", 0.30f, 0, 2))),
                new[] { (ironOre, 0.6m, 2, 4), (copperOre, 0.4m, 1, 3) }),
            (Monster("Harpy", "Attacks from the sky.", "🦅", 16, "Mountains", 200, 14, 8, 10, 12, 12, 22, Element.Lightning, 210, 35, 60,
                SA(("Blind", 0.35f, 0, 2), ("Confusion", 0.20f, 0, 2))),
                new[] { (smallMpPotion, 0.3m, 1, 1), (oakBow, 0.05m, 1, 1) }),
            (Monster("Mountain Ogre", "Shakes the ground when walking.", "👾", 19, "Mountains", 320, 20, 14, 5, 4, 18, 30, Element.None, 280, 45, 80,
                SA(("AttackDown", 0.30f, 6, 3), ("DefenseDown", 0.25f, 5, 2))),
                new[] { (steelAxe, 0.03m, 1, 1), (hpPotion, 0.25m, 1, 1) }),

            (Monster("Skeleton Knight", "A cursed warrior risen.", "💀", 24, "Dungeon", 380, 22, 18, 8, 8, 20, 32, Element.Dark, 400, 55, 100,
                SA(("Bleed", 0.40f, 12, 3), ("DefenseDown", 0.30f, 8, 2))),
                new[] { (ironHelmet, 0.05m, 1, 1), (ironSword, 0.08m, 1, 1) }),
            (Monster("Dark Mage", "Casts forbidden spells.", "🧙", 28, "Dungeon", 300, 12, 10, 22, 10, 18, 35, Element.Dark, 500, 70, 120,
                SA(("Curse", 0.40f, 0, 3), ("Silence", 0.35f, 0, 2), ("MpDrain", 0.30f, 8, 2))),
                new[] { (apprenticeStaff, 0.08m, 1, 1), (mpPotion, 0.3m, 1, 1) }),
            (Monster("Minotaur", "Guards the labyrinth.", "🐂", 33, "Dungeon", 500, 28, 22, 8, 6, 25, 40, Element.None, 650, 90, 160,
                SA(("AttackDown", 0.35f, 8, 3), ("Confusion", 0.25f, 0, 2))),
                new[] { (steelSword, 0.05m, 1, 1), (hpPotion, 0.3m, 1, 2) }),

            (Monster("Fire Elemental", "Pure living flame.", "🔥", 38, "Volcano", 420, 24, 15, 20, 12, 28, 45, Element.Fire, 800, 120, 200,
                SA(("Burn", 0.55f, 15, 3))),
                new[] { (fireBlade, 0.02m, 1, 1), (ironOre, 0.5m, 2, 5) }),
            (Monster("Lava Wyrm", "A serpent of molten rock.", "🐉", 43, "Volcano", 600, 30, 25, 15, 10, 35, 55, Element.Fire, 1000, 150, 250,
                SA(("Burn", 0.50f, 20, 3), ("DefenseDown", 0.30f, 10, 2))),
                new[] { (hpPotion, 0.4m, 1, 2), (mpPotion, 0.3m, 1, 1) }),
            (Monster("Inferno Drake", "Breathes rivers of fire.", "🐲", 48, "Volcano", 750, 35, 28, 18, 14, 40, 60, Element.Fire, 1300, 200, 350,
                SA(("Burn", 0.55f, 25, 4), ("Slow", 0.30f, 0, 2), ("Curse", 0.25f, 0, 3))),
                new[] { (fireBlade, 0.05m, 1, 1), (thunderStaff, 0.02m, 1, 1) }),

            (Monster("Shadow Demon", "Born from the abyss.", "👿", 55, "Abyss", 900, 40, 30, 30, 20, 45, 70, Element.Dark, 1800, 300, 500,
                SA(("Curse", 0.50f, 0, 4), ("Silence", 0.40f, 0, 3), ("Confusion", 0.35f, 0, 2), ("MpDrain", 0.40f, 15, 3))),
                new[] { (thunderStaff, 0.03m, 1, 1), (frostBow, 0.03m, 1, 1) }),
            (Monster("Ancient Dragon", "The ultimate challenge.", "🐉", 70, "Abyss", 1500, 55, 45, 40, 25, 60, 100, Element.Fire, 5000, 500, 1000,
                SA(("Burn", 0.50f, 40, 4), ("DefenseDown", 0.40f, 15, 3), ("AttackDown", 0.35f, 15, 3), ("Freeze", 0.30f, 30, 1))),
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
        Element element, long xpReward, long orbMin, long orbMax, string? abilityJson = null)
    {
        return new MonsterDefinition
        {
            Name = name, Description = desc, Icon = icon, Level = level, Zone = zone,
            MaxHp = maxHp, STR = str, DEF = def, INT = @int, DEX = dex,
            MinDamage = minDmg, MaxDamage = maxDmg, Element = element,
            XpReward = xpReward, OrbRewardMin = orbMin, OrbRewardMax = orbMax,
            AbilityJson = abilityJson
        };
    }

    // Shorthand for StatusEffects.Abilities(...)
    private static string SA(params (string type, float chance, int strength, int turns)[] abilities) =>
        StatusEffects.Abilities(abilities);

    private async Task PatchConsumablesAsync()
    {
        var all = await _uow.ItemDefinitions.GetAllAsync();
        if (all.Count == 0) return;

        // HealAmount = % of player MaxHp; ManaRestoreAmount = % of player MaxMp
        var map = new Dictionary<string, (int heal, int mana, string desc)>(StringComparer.OrdinalIgnoreCase)
        {
            // Potions
            ["Small Health Potion"]  = (15,  0, "Restores 15% of your max HP."),
            ["Health Potion"]        = (30,  0, "Restores 30% of your max HP."),
            ["Large Health Potion"]  = (50,  0, "Restores 50% of your max HP."),
            ["Small Mana Potion"]    = ( 0, 15, "Restores 15% of your max MP."),
            ["Mana Potion"]          = ( 0, 30, "Restores 30% of your max MP."),
            ["Large Mana Potion"]    = ( 0, 55, "Restores 55% of your max MP."),
            ["Elixir of Life"]       = (75, 60, "Restores 75% HP and 60% MP."),
            // Food
            ["Burnt Fish"]           = ( 1,  0, "Restores 1% HP. Tastes terrible."),
            ["Cooked Shrimp"]        = ( 5,  0, "Restores 5% of your max HP."),
            ["Cooked Trout"]         = ( 8,  0, "Restores 8% of your max HP."),
            ["Cooked Salmon"]        = (12,  0, "Restores 12% of your max HP."),
            ["Cooked Tuna"]          = (18,  0, "Restores 18% of your max HP."),
            ["Fish Stew"]            = (15, 10, "Restores 15% HP and 10% MP."),
            ["Cooked Lobster"]       = (25,  0, "Restores 25% of your max HP."),
            ["Cooked Swordfish"]     = (32,  0, "Restores 32% of your max HP."),
            ["Cooked Shark"]         = (45,  0, "Restores 45% of your max HP."),
            ["Cooked Abyssal Eel"]   = (60, 20, "Restores 60% HP and 20% MP."),
        };

        bool changed = false;
        foreach (var item in all)
        {
            if (!map.TryGetValue(item.Name, out var v)) continue;
            if (item.HealAmount == v.heal && item.ManaRestoreAmount == v.mana && item.Description == v.desc) continue;
            item.HealAmount          = v.heal;
            item.ManaRestoreAmount   = v.mana;
            item.Description         = v.desc;
            changed = true;
        }
        if (changed) await _uow.SaveChangesAsync();
    }

    private async Task PatchMonsterAbilitiesAsync()
    {
        var monsters = await _uow.MonsterDefinitions.GetByLevelRangeAsync(0, 999);
        if (monsters.Count == 0) return;

        // Map monster name → ability JSON. Only fills in monsters that have AbilityJson == null.
        var abilityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Plains (Lv 1-5) ──────────────────────────────────────────────
            ["Slime"]               = SA(("Slow",        0.20f, 0,  2)),
            ["Blue Slime"]          = SA(("Slow",        0.25f, 0,  2), ("Poison",      0.15f, 2,  2)),
            ["Red Slime"]           = SA(("Burn",        0.30f, 4,  2)),
            ["Goblin"]              = SA(("Bleed",       0.30f, 4,  2)),
            ["Cave Goblin"]         = SA(("Bleed",       0.35f, 5,  2), ("Blind",       0.20f, 0,  1)),
            ["Wolf"]                = SA(("Bleed",       0.40f, 6,  3)),
            ["Young Werewolf"]      = SA(("Bleed",       0.35f, 5,  2), ("AttackDown",  0.20f, 3,  2)),

            // ── Forest (Lv 7-12) ─────────────────────────────────────────────
            ["Giant Spider"]        = SA(("Poison",      0.50f, 3,  3)),
            ["Clay Golem"]          = SA(("DefenseDown", 0.30f, 4,  2), ("Slow",        0.25f, 0,  2), ("PhysResist", 1.0f, 15, 0)),
            ["Swamp Hag"]           = SA(("Curse",       0.35f, 0,  3), ("Poison",      0.30f, 2,  3)),
            ["Orc"]                 = SA(("AttackDown",  0.30f, 4,  2)),
            ["Skeleton"]            = SA(("DefenseDown", 0.30f, 4,  2), ("PhysResist",  1.0f, 10, 0)),
            ["Forest Troll"]        = SA(("Bleed",       0.45f, 8,  3), ("Slow",        0.30f, 0,  2)),
            ["Hobgoblin"]           = SA(("Bleed",       0.35f, 6,  2), ("AttackDown",  0.25f, 4,  2)),
            ["Imp"]                 = SA(("Confusion",   0.35f, 0,  2), ("MpDrain",     0.30f, 5,  2)),
            ["Swamp Ogre"]          = SA(("Poison",      0.40f, 3,  3), ("DefenseDown", 0.25f, 5,  2)),

            // ── Mountains (Lv 13-19) ─────────────────────────────────────────
            ["Purple Slime"]        = SA(("Poison",      0.40f, 3,  3), ("Slow",        0.30f, 0,  2)),
            ["Rock Golem"]          = SA(("DefenseDown", 0.35f, 5,  3), ("Slow",        0.30f, 0,  2), ("PhysResist", 1.0f, 20, 0)),
            ["Lost Soul"]           = SA(("Curse",       0.40f, 0,  3), ("MpDrain",     0.35f, 5,  2)),
            ["Silverback Werewolf"] = SA(("Bleed",       0.45f, 8,  3), ("AttackDown",  0.30f, 5,  2)),
            ["Forest Witch"]        = SA(("Curse",       0.40f, 0,  3), ("Silence",     0.30f, 0,  2), ("MpDrain",    0.25f, 6, 2)),
            ["King Slime Prime"]    = SA(("Slow",        0.60f, 0,  3), ("Poison",      0.50f, 4,  3), ("DefenseDown",0.40f, 6, 2)),  // boss
            ["Skeleton Archer"]     = SA(("Blind",       0.40f, 0,  2), ("Bleed",       0.35f, 7,  2), ("PhysResist", 1.0f, 10, 0)),
            ["Cave Troll"]          = SA(("Bleed",       0.40f, 9,  3), ("DefenseDown", 0.30f, 5,  2)),
            ["Harpy"]               = SA(("Blind",       0.40f, 0,  2), ("Confusion",   0.25f, 0,  2)),
            ["Goblin Shaman"]       = SA(("Curse",       0.35f, 0,  3), ("Silence",     0.30f, 0,  2)),
            ["Fledgling Vampire"]   = SA(("MpDrain",     0.40f, 6,  2), ("Bleed",       0.30f, 6,  2)),
            ["Wyvern"]              = SA(("Burn",        0.35f, 8,  2), ("Blind",       0.25f, 0,  2)),
            ["Mountain Ogre"]       = SA(("AttackDown",  0.35f, 6,  3), ("DefenseDown", 0.30f, 5,  2)),

            // ── Dungeon (Lv 20-33) ───────────────────────────────────────────
            ["Crystal Slime"]       = SA(("Slow",        0.45f, 0,  3), ("DefenseDown", 0.35f, 6,  2), ("PhysResist", 1.0f, 15, 0)),
            ["Goblin Overlord"]     = SA(("AttackDown",  0.50f, 8,  3), ("DefenseDown", 0.45f, 8,  2), ("Confusion",  0.35f, 0, 2)),  // boss
            ["Wailing Banshee"]     = SA(("Confusion",   0.45f, 0,  2), ("Silence",     0.40f, 0,  2), ("Curse",      0.30f, 0, 3)),
            ["Mountain Troll"]      = SA(("Bleed",       0.45f, 12, 3), ("Slow",        0.35f, 0,  2), ("DefenseDown",0.30f, 6, 2)),
            ["Iron Golem"]          = SA(("DefenseDown", 0.35f, 8,  3), ("Slow",        0.40f, 0,  2), ("PhysResist", 1.0f, 30, 0)),
            ["Alpha Werewolf"]      = SA(("Bleed",       0.50f, 12, 3), ("AttackDown",  0.35f, 6,  2), ("Confusion",  0.25f, 0, 2)),
            ["Shadow Witch"]        = SA(("Silence",     0.45f, 0,  3), ("Curse",       0.40f, 0,  3), ("MpDrain",    0.35f, 8, 2)),
            ["Skeleton Knight"]     = SA(("Bleed",       0.45f, 12, 3), ("DefenseDown", 0.35f, 8,  2), ("PhysResist", 1.0f, 10, 0)),
            ["Fire Demon"]          = SA(("Burn",        0.50f, 14, 3), ("AttackDown",  0.30f, 6,  2)),
            ["Elder Forest Troll"]  = SA(("Bleed",       0.55f, 14, 4), ("Bleed",       0.40f, 10, 3), ("Slow",       0.45f, 0, 2), ("DefenseDown", 0.35f, 8, 2)),  // boss — piles bleed
            ["Goblin Warchief"]     = SA(("AttackDown",  0.40f, 7,  3), ("Confusion",   0.30f, 0,  2), ("DefenseDown",0.25f, 6, 2)),
            ["Cave Ogre"]           = SA(("DefenseDown", 0.40f, 7,  3), ("Slow",        0.35f, 0,  2)),
            ["Green Dragon"]        = SA(("Burn",        0.40f, 12, 3), ("Blind",       0.30f, 0,  2)),
            ["Dark Mage"]           = SA(("Curse",       0.45f, 0,  3), ("Silence",     0.40f, 0,  2), ("MpDrain",    0.35f, 8, 2), ("MagicResist", 1.0f, 15, 0)),
            ["Noble Vampire"]       = SA(("MpDrain",     0.45f, 8,  3), ("Curse",       0.40f, 0,  3), ("Bleed",      0.40f, 10, 3)),
            ["Skeleton Mage"]       = SA(("Silence",     0.45f, 0,  3), ("MpDrain",     0.40f, 8,  2), ("Curse",      0.35f, 0, 3), ("PhysResist", 1.0f, 10, 0), ("MagicResist", 1.0f, 15, 0)),
            ["Death Screamer"]      = SA(("Confusion",   0.50f, 0,  3), ("Silence",     0.40f, 0,  2)),
            ["Ice Demon"]           = SA(("Freeze",      0.45f, 15, 2), ("Slow",        0.40f, 0,  3)),
            ["Frost Troll"]         = SA(("Bleed",       0.45f, 12, 3), ("Freeze",      0.35f, 12, 2)),
            ["Dungeon Warden"]      = SA(("DefenseDown", 0.55f, 10, 3), ("AttackDown",  0.50f, 10, 3), ("Slow",       0.45f, 0, 2), ("Curse", 0.35f, 0, 3)),  // boss
            ["Minotaur"]            = SA(("AttackDown",  0.40f, 8,  3), ("Confusion",   0.30f, 0,  2)),

            // ── Volcano (Lv 34-48) ───────────────────────────────────────────
            ["Hex Mistress"]        = SA(("Curse",       0.55f, 0,  4), ("Confusion",   0.45f, 0,  3), ("Silence",    0.40f, 0, 2), ("AttackDown", 0.35f, 8, 3)),
            ["Lunar Werewolf"]      = SA(("Bleed",       0.50f, 14, 3), ("AttackDown",  0.40f, 8,  2), ("Confusion",  0.35f, 0, 2)),
            ["Frost Ogre"]          = SA(("Freeze",      0.45f, 18, 2), ("DefenseDown", 0.40f, 8,  2)),
            ["Slime King"]          = SA(("Slow",        0.55f, 0,  3), ("Poison",      0.50f, 4,  4), ("Poison",     0.35f, 3, 3)),  // piles poison
            ["Obsidian Golem"]      = SA(("DefenseDown", 0.40f, 10, 3), ("Slow",        0.45f, 0,  3), ("PhysResist", 1.0f, 25, 0), ("MagicResist", 1.0f, 10, 0)),
            ["Blood Hunter"]        = SA(("Bleed",       0.55f, 14, 3), ("Curse",       0.40f, 0,  3), ("MpDrain",    0.35f, 10, 2)),
            ["Fire Elemental"]      = SA(("Burn",        0.60f, 15, 3), ("MagicResist", 1.0f, 25, 0)),
            ["Blue Dragon"]         = SA(("Freeze",      0.45f, 20, 2), ("Confusion",   0.35f, 0,  2), ("Slow",       0.30f, 0, 2)),
            ["Volcanic Titan"]      = SA(("Burn",        0.60f, 20, 4), ("Burn",        0.45f, 15, 3), ("DefenseDown",0.50f, 12, 3), ("AttackDown", 0.40f, 12, 2), ("Slow", 0.35f, 0, 2)),  // boss — piles burn
            ["Ancient Troll"]       = SA(("Bleed",       0.50f, 16, 4), ("Slow",        0.45f, 0,  3), ("DefenseDown",0.40f, 10, 2)),
            ["Phantom Banshee"]     = SA(("Confusion",   0.50f, 0,  3), ("Silence",     0.45f, 0,  3), ("Curse",      0.40f, 0, 4), ("MagicResist", 1.0f, 20, 0)),
            ["Lava Wyrm"]           = SA(("Burn",        0.55f, 20, 3), ("DefenseDown", 0.35f, 10, 2)),
            ["Cyclops"]             = SA(("DefenseDown", 0.45f, 12, 3), ("Blind",       0.50f, 0,  2), ("Slow",       0.35f, 0, 2)),
            ["Grand Sorceress"]     = SA(("MpDrain",     0.55f, 12, 3), ("Silence",     0.50f, 0,  3), ("Curse",      0.45f, 0, 4), ("AttackDown", 0.40f, 10, 2), ("MagicResist", 1.0f, 25, 0)),
            ["Lich"]                = SA(("Curse",       0.55f, 0,  4), ("MpDrain",     0.50f, 12, 3), ("Poison",     0.45f, 4, 3), ("DefenseDown", 0.40f, 10, 2), ("PhysResist", 1.0f, 15, 0), ("MagicResist", 1.0f, 20, 0)),
            ["Vampire Lord"]        = SA(("MpDrain",     0.55f, 14, 3), ("Bleed",       0.50f, 16, 3), ("Curse",      0.45f, 0, 4)),
            ["Lava Golem"]          = SA(("Burn",        0.55f, 22, 3), ("DefenseDown", 0.40f, 12, 3), ("PhysResist", 1.0f, 20, 0), ("MagicResist", 1.0f, 10, 0)),
            ["Inferno Drake"]       = SA(("Burn",        0.60f, 25, 4), ("Slow",        0.35f, 0,  2), ("Curse",      0.30f, 0, 3)),

            // ── Abyss (Lv 50-90) ─────────────────────────────────────────────
            ["Blood Moon Werewolf"] = SA(("Bleed",       0.60f, 18, 4), ("Bleed",       0.45f, 14, 3), ("AttackDown", 0.45f, 12, 2), ("Confusion", 0.35f, 0, 3)),  // piles bleed
            ["Abyssal Overlord"]    = SA(("Curse",       0.65f, 0,  5), ("Silence",     0.55f, 0,  4), ("Confusion",  0.50f, 0, 3), ("MpDrain", 0.55f, 18, 4), ("DefenseDown", 0.50f, 15, 3), ("AttackDown", 0.45f, 15, 3)),  // boss
            ["Red Dragon"]          = SA(("Burn",        0.55f, 30, 4), ("Burn",        0.40f, 20, 3), ("DefenseDown",0.40f, 12, 3), ("AttackDown", 0.35f, 12, 2)),  // piles burn
            ["Shadow Demon"]        = SA(("Curse",       0.55f, 0,  4), ("Silence",     0.45f, 0,  3), ("Confusion",  0.40f, 0, 3), ("MpDrain",   0.45f, 15, 3), ("MagicResist", 1.0f, 30, 0)),
            ["Queen Banshee"]       = SA(("Confusion",   0.60f, 0,  4), ("Silence",     0.55f, 0,  4), ("Curse",      0.50f, 0, 5), ("MpDrain",   0.45f, 18, 3), ("MagicResist", 1.0f, 20, 0)),
            ["Lich King"]           = SA(("Curse",       0.70f, 0,  5), ("MpDrain",     0.65f, 20, 4), ("Silence",    0.55f, 0, 4), ("Poison",    0.50f, 6,  4), ("DefenseDown", 0.50f, 15, 3), ("PhysResist", 1.0f, 20, 0), ("MagicResist", 1.0f, 30, 0)),  // boss
            ["Ancient Vampire"]     = SA(("MpDrain",     0.60f, 18, 4), ("Bleed",       0.55f, 20, 4), ("Curse",      0.55f, 0, 5), ("Confusion", 0.40f, 0,  3)),
            ["Black Dragon"]        = SA(("Poison",      0.55f, 6,  4), ("Poison",      0.45f, 5,  3), ("Blind",      0.50f, 0, 3), ("DefenseDown", 0.45f, 15, 3), ("AttackDown", 0.40f, 15, 3)),  // piles poison
            ["Archdemon"]           = SA(("Curse",       0.65f, 0,  5), ("Confusion",   0.55f, 0,  4), ("MpDrain",    0.55f, 18, 4), ("Burn",      0.50f, 30, 4), ("AttackDown", 0.50f, 15, 3), ("MagicResist", 1.0f, 25, 0)),
            ["Ancient Dragon"]      = SA(("Burn",        0.55f, 40, 4), ("DefenseDown", 0.45f, 15, 3), ("AttackDown", 0.40f, 15, 3), ("Freeze",    0.35f, 30, 1), ("PhysResist", 1.0f, 15, 0), ("MagicResist", 1.0f, 15, 0)),
            ["Primordial Dragon"]   = SA(("Burn",        0.70f, 50, 5), ("Burn",        0.55f, 35, 4), ("DefenseDown",0.60f, 20, 4), ("AttackDown",0.55f, 20, 4), ("Confusion",  0.45f, 0, 3), ("PhysResist", 1.0f, 20, 0), ("MagicResist", 1.0f, 20, 0)),  // boss
            ["World Serpent"]       = SA(("Poison",      0.70f, 8,  6), ("Poison",      0.60f, 6,  5), ("Blind",      0.60f, 0, 4), ("Confusion", 0.55f, 0,  4), ("Slow",       0.55f, 0, 4), ("Curse",       0.50f, 0, 5), ("DefenseDown", 0.55f, 20, 4), ("PhysResist", 1.0f, 25, 0), ("MagicResist", 1.0f, 25, 0)),  // boss
        };

        bool changed = false;
        foreach (var m in monsters)
        {
            if (!abilityMap.TryGetValue(m.Name, out var json)) continue;
            if (m.AbilityJson == json) continue;
            m.AbilityJson = json;
            changed = true;
        }
        if (changed) await _uow.SaveChangesAsync();
    }
}
