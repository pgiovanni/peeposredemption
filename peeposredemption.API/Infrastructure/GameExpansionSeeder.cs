using Microsoft.EntityFrameworkCore;
using peeposredemption.Domain.Entities;
using peeposredemption.Infrastructure.Persistence;

namespace peeposredemption.API.Infrastructure;

/// <summary>
/// Adds monster/item expansion content without touching already-seeded data.
/// Safe to run on every startup — checks by name before inserting.
/// </summary>
public static class GameExpansionSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existingItems    = await db.ItemDefinitions.ToDictionaryAsync(i => i.Name, ct);
        var existingMonsters = await db.MonsterDefinitions.Select(m => m.Name).ToHashSetAsync(ct);

        var newItems = new List<ItemDefinition>();

        ItemDefinition GetOrCreate(string name, Func<ItemDefinition> factory)
        {
            if (existingItems.TryGetValue(name, out var existing)) return existing;
            var item = factory();
            newItems.Add(item);
            existingItems[name] = item;
            return item;
        }

        // ── NEW WEAPONS ────────────────────────────────────────────────────────
        var silverSword   = GetOrCreate("Silver Sword",    () => W("Silver Sword",    "Blessed silver — lethal to undead and werewolves.",  "🗡️", 15, 12, 22, str: 4,  buy: 800,   sell: 250,  rarity: GameItemRarity.Uncommon));
        var shadowDagger  = GetOrCreate("Shadow Dagger",   () => W("Shadow Dagger",   "Drinks in the darkness.",                            "🖤", 20, 14, 24, dex: 3, luk: 3,  buy: 1200,  sell: 400,  element: Element.Dark));
        var moonblade     = GetOrCreate("Moonblade",       () => W("Moonblade",       "Glows cold under the full moon.",                    "🌙", 30, 22, 38, str: 5, luk: 3,  buy: 2000,  sell: 700));
        var boneCrusher   = GetOrCreate("Bone Crusher",    () => W("Bone Crusher",    "A massive club that pulverises bone.",               "🪨", 25, 18, 30, str: 7,          buy: 1200,  sell: 400,  rarity: GameItemRarity.Uncommon));
        var holyLance     = GetOrCreate("Holy Lance",      () => W("Holy Lance",      "Radiates divine wrath.",                            "✝️", 35, 25, 42, str: 5, vit: 3,  buy: 2500,  sell: 800,  element: Element.Holy));
        var frostSword    = GetOrCreate("Frost Sword",     () => W("Frost Sword",     "The blade is always cold to the touch.",            "🧊", 28, 20, 34, str: 5, def: 2,  buy: 1800,  sell: 600,  element: Element.Ice));
        var voidStaff     = GetOrCreate("Void Staff",      () => W("Void Staff",      "Channels power from the abyss itself.",             "🌑", 45, 35, 55, @int: 12,        buy: 5000,  sell: 1800, element: Element.Dark,      subType: ItemSubType.Staff, rarity: GameItemRarity.Epic));
        var dragonLance   = GetOrCreate("Dragon Lance",    () => W("Dragon Lance",    "Forged from a dragon's shattered fang.",            "🐉", 55, 45, 70, str: 10, def: 5, buy: 8000,  sell: 3000, element: Element.Fire,      rarity: GameItemRarity.Epic));
        var stormBow      = GetOrCreate("Storm Bow",       () => W("Storm Bow",       "Arrows crackle with lightning on release.",         "⚡", 38, 28, 45, dex: 8,          buy: 3000,  sell: 1000, element: Element.Lightning, subType: ItemSubType.Bow));
        var obsidianBlade = GetOrCreate("Obsidian Blade",  () => W("Obsidian Blade",  "Razor-sharp volcanic glass.",                       "🔴", 42, 32, 50, str: 8, @int: 3, buy: 3500,  sell: 1200, element: Element.Fire));

        // ── STEEL ARMOR SET (Lv 20) ────────────────────────────────────────────
        var steelHelmet = GetOrCreate("Steel Helmet",     () => Ar("Steel Helmet",     "Forged steel headpiece.",        "⛑️", 20, EquipSlot.Head,  GameItemRarity.Uncommon, def: 7,  vit: 1, buy: 500,  sell: 150));
        var steelChest  = GetOrCreate("Steel Chestplate", () => Ar("Steel Chestplate", "Solid plate armour.",            "🛡️", 20, EquipSlot.Chest, GameItemRarity.Uncommon, def: 12, vit: 2, buy: 900,  sell: 270));
        var steelLegs   = GetOrCreate("Steel Leggings",   () => Ar("Steel Leggings",   "Heavy leg plates.",              "🦿", 20, EquipSlot.Legs,  GameItemRarity.Uncommon, def: 9,  vit: 1, buy: 700,  sell: 210));
        var steelBoots  = GetOrCreate("Steel Boots",      () => Ar("Steel Boots",      "Reinforced steel footwear.",     "🥾", 20, EquipSlot.Feet,  GameItemRarity.Uncommon, def: 5,  dex: 1, buy: 350,  sell: 105));

        // ── SHADOW ARMOR SET (Lv 35) ───────────────────────────────────────────
        var shadowCowl    = GetOrCreate("Shadow Cowl",    () => Ar("Shadow Cowl",    "A hood woven from pure darkness.",  "🎭", 35, EquipSlot.Head,  GameItemRarity.Rare, def: 8,  @int: 4, luk: 2, buy: 2000, sell: 700));
        var shadowRobe    = GetOrCreate("Shadow Robe",    () => Ar("Shadow Robe",    "Shifts between shadows.",           "🌑", 35, EquipSlot.Chest, GameItemRarity.Rare, def: 13, @int: 6, luk: 2, buy: 3500, sell: 1200));
        var shadowGreaves = GetOrCreate("Shadow Greaves", () => Ar("Shadow Greaves", "Silent, weightless steps.",         "👢", 35, EquipSlot.Legs,  GameItemRarity.Rare, def: 10, dex: 4,  luk: 2, buy: 2500, sell: 900));
        var shadowBoots   = GetOrCreate("Shadow Boots",   () => Ar("Shadow Boots",   "Leaves no footprints.",            "👟", 35, EquipSlot.Feet,  GameItemRarity.Rare, def: 7,  dex: 3,  luk: 1, buy: 1500, sell: 500));

        // ── DRAGONSCALE ARMOR SET (Lv 50) ─────────────────────────────────────
        var dsHelmet = GetOrCreate("Dragonscale Helmet",     () => Ar("Dragonscale Helmet",     "Scales of an ancient dragon.",  "🐲", 50, EquipSlot.Head,  GameItemRarity.Epic, def: 15, vit: 5, str: 3, buy: 6000,  sell: 2000));
        var dsChest  = GetOrCreate("Dragonscale Chestplate", () => Ar("Dragonscale Chestplate", "Nigh-impenetrable plate.",      "🛡️", 50, EquipSlot.Chest, GameItemRarity.Epic, def: 25, vit: 8, str: 5, buy: 10000, sell: 3500));
        var dsLegs   = GetOrCreate("Dragonscale Leggings",   () => Ar("Dragonscale Leggings",   "Heavy fireproof leg plates.",   "🦿", 50, EquipSlot.Legs,  GameItemRarity.Epic, def: 18, vit: 6, str: 4, buy: 7500,  sell: 2500));
        var dsBoots  = GetOrCreate("Dragonscale Boots",      () => Ar("Dragonscale Boots",      "Clawed dragon boots.",          "🥾", 50, EquipSlot.Feet,  GameItemRarity.Epic, def: 10, vit: 4, dex: 2, buy: 4000,  sell: 1400));

        // ── NEW CONSUMABLES ────────────────────────────────────────────────────
        var largeHpPot = GetOrCreate("Large Health Potion", () => Co("Large Health Potion", "Restores 100 HP.", "🧪", 20, heal: 100, buy: 100, sell: 35));
        var largeMpPot = GetOrCreate("Large Mana Potion",   () => Co("Large Mana Potion",   "Restores 80 MP.",  "💧", 20, mana: 80,  buy: 100, sell: 35));
        var elixir     = GetOrCreate("Elixir of Life",      () => Co("Elixir of Life",      "Restores 200 HP and 150 MP.", "⚗️", 40, heal: 200, mana: 150, rarity: GameItemRarity.Rare, buy: 500, sell: 175));

        // ── NEW MATERIALS ──────────────────────────────────────────────────────
        var slimeCore     = GetOrCreate("Slime Core",     () => Ma("Slime Core",     "A gelatinous orb from a slime.",    "🔵", buy: 8,   sell: 3));
        var goblinEar     = GetOrCreate("Goblin Ear",     () => Ma("Goblin Ear",     "A pointy goblin ear.",              "👂", buy: 10,  sell: 4));
        var trollHide     = GetOrCreate("Troll Hide",     () => Ma("Troll Hide",     "Thick, pungent hide.",              "🟫", buy: 20,  sell: 8));
        var demonHorn     = GetOrCreate("Demon Horn",     () => Ma("Demon Horn",     "A twisted horn from a demon.",      "📯", buy: 50,  sell: 20,  rarity: GameItemRarity.Uncommon));
        var dragonScale   = GetOrCreate("Dragon Scale",   () => Ma("Dragon Scale",   "A shimmering scale.",               "🐉", buy: 150, sell: 60,  rarity: GameItemRarity.Rare));
        var werewolfPelt  = GetOrCreate("Werewolf Pelt",  () => Ma("Werewolf Pelt",  "Thick fur, still smells wild.",     "🐾", buy: 25,  sell: 10));
        var skullFragment = GetOrCreate("Skull Fragment", () => Ma("Skull Fragment", "A cracked bone fragment.",          "💀", buy: 15,  sell: 6));
        var vampireFang   = GetOrCreate("Vampire Fang",   () => Ma("Vampire Fang",   "Sharp and bloodstained.",           "🦷", buy: 40,  sell: 15,  rarity: GameItemRarity.Uncommon));
        var golemCore     = GetOrCreate("Golem Core",     () => Ma("Golem Core",     "The animating heart of a golem.",   "⚙️", buy: 35,  sell: 14,  rarity: GameItemRarity.Uncommon));
        var witchHerb     = GetOrCreate("Witch Herb",     () => Ma("Witch Herb",     "Glows faintly green.",              "🌿", buy: 20,  sell: 8));
        var bansheeWisp   = GetOrCreate("Banshee Wisp",   () => Ma("Banshee Wisp",   "A lingering spectral echo.",        "🌫️", buy: 30,  sell: 12,  rarity: GameItemRarity.Uncommon));
        var ogreTusk      = GetOrCreate("Ogre Tusk",      () => Ma("Ogre Tusk",      "A massive yellowed tusk.",          "🦴", buy: 18,  sell: 7));
        var dragonClaw    = GetOrCreate("Dragon Claw",    () => Ma("Dragon Claw",    "Razor-edged claw.",                 "🐾", buy: 200, sell: 80,  rarity: GameItemRarity.Rare));
        var lichDust      = GetOrCreate("Lich Dust",      () => Ma("Lich Dust",      "Ancient necrotic powder.",          "💨", buy: 100, sell: 40,  rarity: GameItemRarity.Rare));

        if (newItems.Count > 0)
        {
            db.ItemDefinitions.AddRange(newItems);
            await db.SaveChangesAsync(ct);
        }

        // Base-seeder items referenced in loot tables
        var smallHpPotion = existingItems.GetValueOrDefault("Small Health Potion");
        var hpPotion      = existingItems.GetValueOrDefault("Health Potion");
        var smallMpPotion = existingItems.GetValueOrDefault("Small Mana Potion");
        var mpPotion      = existingItems.GetValueOrDefault("Mana Potion");
        var ironSword     = existingItems.GetValueOrDefault("Iron Sword");
        var steelSword    = existingItems.GetValueOrDefault("Steel Sword");
        var ironOre       = existingItems.GetValueOrDefault("Iron Ore");
        var copperOre     = existingItems.GetValueOrDefault("Copper Ore");
        var wood          = existingItems.GetValueOrDefault("Wood");
        var leather       = existingItems.GetValueOrDefault("Leather");
        var fireBlade     = existingItems.GetValueOrDefault("Fire Blade");
        var frostBow      = existingItems.GetValueOrDefault("Frost Bow");

        // ── NEW MONSTERS ───────────────────────────────────────────────────────
        var pending = new List<(MonsterDefinition m, (ItemDefinition? item, decimal chance, int min, int max)[] loot)>();

        void Add(MonsterDefinition m, params (ItemDefinition?, decimal, int, int)[] loot)
        {
            if (!existingMonsters.Contains(m.Name))
                pending.Add((m, loot));
        }

        // SLIMES (Slime Lv1 exists)
        Add(Mo("Blue Slime",    "A cool, ice-tinged blob.",         "🔵",  4, "Plains",    84,  3, 2,  3, 2,  5,  8, Element.Ice,         60,  8,  30),
            (slimeCore, 0.50m, 1, 1), (smallMpPotion, 0.25m, 1, 1));
        Add(Mo("Red Slime",     "Burns on contact.",                "🔴",  8, "Forest",   156,  5, 4,  6, 3, 10, 16, Element.Fire,        110, 16,  55),
            (slimeCore, 0.55m, 1, 2), (smallHpPotion, 0.20m, 1, 1));
        Add(Mo("Purple Slime",  "Oozes dark toxins.",              "🟣", 13, "Forest",   246,  8, 7,  9, 5, 16, 26, Element.Dark,        195, 26,  90),
            (slimeCore, 0.60m, 1, 2), (smallHpPotion, 0.15m, 1, 1));
        Add(Mo("Crystal Slime", "Crackles with electricity.",      "💎", 20, "Mountains", 460, 13, 10, 14, 9, 24, 40, Element.Lightning,  390, 40, 170),
            (slimeCore, 0.50m, 1, 3), (smallMpPotion, 0.30m, 1, 1));
        Add(Mo("Slime King",    "An enormous, regal blob.",        "👑", 35, "Dungeon",   920, 20, 18, 18, 10, 42, 70, Element.Earth,      820, 70, 320),
            (slimeCore, 0.90m, 2, 5), (largeHpPot, 0.30m, 1, 1), (steelSword, 0.04m, 1, 1));

        // GOBLINS (Goblin Lv3 exists)
        Add(Mo("Cave Goblin",     "Sneaks through the darkness.",    "👺",  6, "Forest",   120,  4, 3,  4, 5,  7, 12, Element.None,         80, 10,  40),
            (goblinEar, 0.55m, 1, 2), (copperOre, 0.25m, 1, 2));
        Add(Mo("Hobgoblin",       "Bigger, meaner cousin.",          "👺", 11, "Mountains", 220, 10, 7,  5, 6, 13, 22, Element.None,        165, 22,  80),
            (goblinEar, 0.50m, 1, 2), (ironOre, 0.30m, 1, 2), (ironSword, 0.04m, 1, 1));
        Add(Mo("Goblin Shaman",   "Calls on dark spirits.",          "🧿", 17, "Dungeon",  315, 10, 9, 15, 7, 20, 34, Element.Dark,         275, 34, 125),
            (goblinEar, 0.40m, 1, 2), (smallMpPotion, 0.35m, 1, 2), (shadowDagger, 0.03m, 1, 1));
        Add(Mo("Goblin Warchief", "Commands entire goblin armies.", "👿", 26, "Dungeon",  590, 22, 15, 10, 14, 31, 52, Element.None,        520, 52, 220),
            (goblinEar, 0.60m, 2, 4), (steelSword, 0.05m, 1, 1), (hpPotion, 0.30m, 1, 2));

        // TROLLS (Forest Troll Lv10 exists)
        Add(Mo("Cave Troll",    "Massive and dim-witted.",          "🧌", 16, "Mountains", 360, 15, 14, 4, 4, 19, 32, Element.None,        250, 32, 115),
            (trollHide, 0.50m, 1, 2), (wood, 0.40m, 1, 3), (hpPotion, 0.15m, 1, 1));
        Add(Mo("Mountain Troll","Hurls boulders from above.",       "🧌", 22, "Mountains", 510, 20, 18, 5, 4, 26, 44, Element.Earth,       400, 44, 185),
            (trollHide, 0.55m, 1, 2), (ironOre, 0.40m, 1, 3), (boneCrusher, 0.03m, 1, 1));
        Add(Mo("Frost Troll",   "Frozen to its icy core.",          "❄️", 30, "Dungeon",  710, 26, 22, 8, 6, 36, 60, Element.Ice,          620, 60, 260),
            (trollHide, 0.60m, 1, 3), (frostSword, 0.04m, 1, 1), (largeHpPot, 0.25m, 1, 1));
        Add(Mo("Ancient Troll", "Survived since the age of gods.",  "🗿", 42, "Volcano", 1040, 35, 30, 10, 8, 50, 84, Element.Earth,      1150, 84, 410),
            (trollHide, 0.70m, 2, 4), (golemCore, 0.15m, 1, 1), (boneCrusher, 0.05m, 1, 1), (largeHpPot, 0.30m, 1, 2));

        // DEMONS (Shadow Demon Lv55 exists)
        Add(Mo("Imp",        "Small, vicious, and unpredictable.",  "😈", 12, "Dungeon",  228,  8, 6,  8, 9, 14, 24, Element.Fire,        180, 24,  80),
            (demonHorn, 0.30m, 1, 1), (smallMpPotion, 0.30m, 1, 1));
        Add(Mo("Fire Demon", "Wreathed in infernal flame.",         "🔥", 25, "Dungeon",  560, 22, 14, 18, 12, 30, 50, Element.Fire,       470, 50, 210),
            (demonHorn, 0.45m, 1, 2), (fireBlade, 0.03m, 1, 1), (mpPotion, 0.20m, 1, 1));
        Add(Mo("Ice Demon",  "Its touch leaves permanent frostbite.", "🧊", 30, "Dungeon", 660, 22, 16, 22, 14, 36, 60, Element.Ice,      620, 60, 260),
            (demonHorn, 0.45m, 1, 2), (frostSword, 0.03m, 1, 1), (largeHpPot, 0.20m, 1, 1));
        Add(Mo("Archdemon",  "A ruler of the deepest abyss.",       "👹", 65, "Abyss",  2120, 52, 42, 48, 30, 78, 130, Element.Dark,      3600, 130, 860),
            (demonHorn, 0.80m, 2, 4), (voidStaff, 0.08m, 1, 1), (elixir, 0.20m, 1, 1), (dragonScale, 0.10m, 1, 1));

        // DRAGONS (Ancient Dragon Lv70 exists)
        Add(Mo("Wyvern",       "A juvenile two-legged dragon.",     "🦎", 18, "Mountains", 385, 17, 13, 10, 12, 22, 36, Element.None,      330, 36, 140),
            (dragonScale, 0.20m, 1, 1), (leather, 0.40m, 1, 2));
        Add(Mo("Green Dragon", "Breathes corrosive gas.",           "🐉", 28, "Forest",   710, 24, 18, 16, 14, 34, 56, Element.Earth,      560, 56, 240),
            (dragonScale, 0.30m, 1, 2), (wood, 0.50m, 2, 4), (frostBow, 0.02m, 1, 1));
        Add(Mo("Blue Dragon",  "Commands storms and thunder.",      "🐲", 40, "Mountains", 995, 30, 22, 25, 22, 48, 80, Element.Lightning, 1050, 80, 390),
            (dragonScale, 0.40m, 1, 2), (stormBow, 0.05m, 1, 1), (largeHpPot, 0.25m, 1, 1));
        Add(Mo("Red Dragon",   "Rivers of fire follow its path.",  "🔥", 52, "Volcano", 1420, 40, 30, 28, 24, 62, 104, Element.Fire,     1950, 104, 610),
            (dragonScale, 0.50m, 1, 3), (fireBlade, 0.08m, 1, 1), (dragonLance, 0.04m, 1, 1), (elixir, 0.15m, 1, 1));
        Add(Mo("Black Dragon", "Darkness bends to its will.",      "🖤", 62, "Abyss",   1930, 46, 36, 42, 28, 74, 124, Element.Dark,     3000, 124, 780),
            (dragonScale, 0.60m, 2, 4), (dragonClaw, 0.40m, 1, 2), (dragonLance, 0.06m, 1, 1), (voidStaff, 0.03m, 1, 1));

        // WEREWOLVES (all new)
        Add(Mo("Young Werewolf",      "Newly turned, barely in control.", "🐺",  8, "Forest",   158,  7, 4, 3,  8, 10, 16, Element.None,    110, 16,  55),
            (werewolfPelt, 0.50m, 1, 1), (leather, 0.30m, 1, 1));
        Add(Mo("Silverback Werewolf", "Silver fur ripples as it moves.", "🐺", 15, "Mountains", 315, 14, 9, 4, 13, 18, 30, Element.None,    240, 30, 110),
            (werewolfPelt, 0.55m, 1, 2), (silverSword, 0.04m, 1, 1));
        Add(Mo("Alpha Werewolf",      "Pack leader, twice as deadly.",   "🐺", 23, "Mountains", 510, 20, 14, 6, 18, 28, 46, Element.None,    415, 46, 195),
            (werewolfPelt, 0.60m, 1, 2), (moonblade, 0.02m, 1, 1), (hpPotion, 0.25m, 1, 1));
        Add(Mo("Lunar Werewolf",      "Empowered by the full moon.",     "🌕", 34, "Dungeon",   770, 28, 19, 10, 24, 41, 68, Element.Dark,   760, 68, 300),
            (werewolfPelt, 0.65m, 2, 3), (moonblade, 0.05m, 1, 1), (largeHpPot, 0.20m, 1, 1));
        Add(Mo("Blood Moon Werewolf", "Its howls shake the abyss.",     "🩸", 50, "Abyss",    1370, 38, 28, 16, 30, 60, 100, Element.Dark,  1800, 100, 570),
            (werewolfPelt, 0.75m, 2, 4), (moonblade, 0.08m, 1, 1), (dragonLance, 0.02m, 1, 1), (elixir, 0.10m, 1, 1));

        // SKELETONS (Skeleton Knight Lv24 exists)
        Add(Mo("Skeleton",        "Bare bones animated by dark magic.", "💀", 10, "Dungeon",  200,  7, 6,  4,  5, 12, 20, Element.Dark,    140, 20,  65),
            (skullFragment, 0.55m, 1, 2), (ironOre, 0.20m, 1, 1));
        Add(Mo("Skeleton Archer", "Rattling arrows never miss.",        "🏹", 16, "Dungeon",  305,  9, 8,  6, 12, 19, 32, Element.Dark,    250, 32, 115),
            (skullFragment, 0.55m, 1, 2), (silverSword, 0.03m, 1, 1));
        Add(Mo("Skeleton Mage",   "Casts curses from hollow sockets.", "💀", 30, "Dungeon",  590, 15, 14, 24,  9, 36, 60, Element.Dark,    620, 60, 260),
            (skullFragment, 0.50m, 2, 3), (mpPotion, 0.30m, 1, 1), (voidStaff, 0.02m, 1, 1));
        Add(Mo("Lich",            "Death incarnate, eternal sorcerer.","💀", 45, "Abyss",   1320, 30, 26, 38, 18, 54, 90, Element.Dark,   1400, 90, 470),
            (skullFragment, 0.70m, 2, 4), (lichDust, 0.50m, 1, 2), (voidStaff, 0.07m, 1, 1), (holyLance, 0.03m, 1, 1), (elixir, 0.20m, 1, 1));

        // VAMPIRES (all new)
        Add(Mo("Fledgling Vampire", "Newly turned, craves blood.",    "🧛", 18, "Dungeon",  355, 15, 10, 12, 13, 22, 36, Element.Dark,    300, 36, 140),
            (vampireFang, 0.40m, 1, 1), (smallHpPotion, 0.30m, 1, 1));
        Add(Mo("Noble Vampire",    "Charming, ancient, deadly.",      "🧛", 28, "Dungeon",  660, 22, 17, 20, 18, 34, 56, Element.Dark,    570, 56, 240),
            (vampireFang, 0.45m, 1, 2), (shadowDagger, 0.05m, 1, 1), (hpPotion, 0.20m, 1, 1));
        Add(Mo("Blood Hunter",     "Hunts with predatory precision.", "🩸", 36, "Dungeon",  835, 30, 22, 18, 26, 43, 72, Element.Dark,    840, 72, 330),
            (vampireFang, 0.50m, 1, 2), (shadowDagger, 0.06m, 1, 1), (shadowCowl, 0.03m, 1, 1));
        Add(Mo("Vampire Lord",     "Commands legions of undead.",     "🧛", 48, "Abyss",   1300, 38, 28, 30, 26, 58, 96, Element.Dark,   1650, 96, 530),
            (vampireFang, 0.60m, 2, 3), (shadowRobe, 0.04m, 1, 1), (voidStaff, 0.03m, 1, 1), (elixir, 0.15m, 1, 1));
        Add(Mo("Ancient Vampire",  "Has fed for centuries.",          "🖤", 62, "Abyss",   1930, 46, 36, 42, 34, 74, 124, Element.Dark,  3000, 124, 790),
            (vampireFang, 0.70m, 2, 4), (shadowRobe, 0.06m, 1, 1), (voidStaff, 0.05m, 1, 1), (lichDust, 0.20m, 1, 1));

        // GOLEMS (Rock Golem Lv14 exists)
        Add(Mo("Clay Golem",     "Crude, slow, but surprisingly tough.", "🪆",  8, "Forest",   185,  6, 8, 2, 2, 10, 16, Element.Earth,   110, 16,  55),
            (golemCore, 0.20m, 1, 1), (copperOre, 0.50m, 1, 3));
        Add(Mo("Iron Golem",     "Forged in fire, dense as iron.",       "⚙️", 22, "Mountains", 610, 18, 24, 4, 3, 26, 44, Element.Earth,  410, 44, 185),
            (golemCore, 0.30m, 1, 1), (ironOre, 0.60m, 2, 4), (steelHelmet, 0.03m, 1, 1));
        Add(Mo("Obsidian Golem", "Black glass body with razor edges.",   "🔳", 36, "Volcano",  935, 29, 30, 8, 5, 43, 72, Element.Fire,   850, 72, 335),
            (golemCore, 0.40m, 1, 2), (obsidianBlade, 0.05m, 1, 1), (ironOre, 0.50m, 2, 5));
        Add(Mo("Lava Golem",     "Drips molten rock with every step.",   "🌋", 48, "Volcano", 1320, 36, 32, 14, 6, 58, 96, Element.Fire,  1620, 96, 535),
            (golemCore, 0.50m, 1, 2), (obsidianBlade, 0.06m, 1, 1), (dsHelmet, 0.02m, 1, 1), (elixir, 0.10m, 1, 1));

        // WITCHES (all new)
        Add(Mo("Swamp Hag",      "Hides in fetid bogs.",            "🧙",  9, "Forest",   178,  5, 5, 10, 5, 11, 18, Element.Dark,       125, 18,  60),
            (witchHerb, 0.55m, 1, 2), (smallMpPotion, 0.30m, 1, 1));
        Add(Mo("Forest Witch",   "Brews twisted concoctions.",      "🧙", 15, "Forest",   305,  8, 8, 16, 8, 18, 30, Element.Dark,       240, 30, 105),
            (witchHerb, 0.55m, 1, 2), (mpPotion, 0.20m, 1, 1), (shadowDagger, 0.02m, 1, 1));
        Add(Mo("Shadow Witch",   "Her hexes linger for days.",      "🌑", 24, "Dungeon",  540, 12, 12, 22, 10, 29, 48, Element.Dark,      490, 48, 210),
            (witchHerb, 0.50m, 2, 3), (mpPotion, 0.30m, 1, 1), (shadowCowl, 0.03m, 1, 1));
        Add(Mo("Hex Mistress",   "Her gaze alone inflicts pain.",   "🌑", 32, "Dungeon",  710, 16, 16, 28, 14, 38, 64, Element.Dark,      700, 64, 280),
            (witchHerb, 0.50m, 2, 3), (mpPotion, 0.35m, 1, 2), (voidStaff, 0.03m, 1, 1), (shadowRobe, 0.03m, 1, 1));
        Add(Mo("Grand Sorceress","Ancient power radiates from her.","✨", 45, "Abyss",   1300, 24, 22, 42, 20, 54, 90, Element.Dark,     1380, 90, 465),
            (witchHerb, 0.60m, 2, 4), (voidStaff, 0.07m, 1, 1), (holyLance, 0.03m, 1, 1), (elixir, 0.20m, 1, 1));

        // BANSHEES (all new)
        Add(Mo("Lost Soul",       "A spirit lost between worlds.",   "👻", 14, "Dungeon",  265,  8, 6, 12, 10, 17, 28, Element.Dark,      210, 28,  95),
            (bansheeWisp, 0.45m, 1, 1), (smallMpPotion, 0.25m, 1, 1));
        Add(Mo("Wailing Banshee", "Her scream drains your will.",    "👻", 21, "Dungeon",  435, 13, 10, 18, 14, 25, 42, Element.Dark,     380, 42, 175),
            (bansheeWisp, 0.50m, 1, 2), (mpPotion, 0.20m, 1, 1), (shadowCowl, 0.02m, 1, 1));
        Add(Mo("Death Screamer",  "The sound itself deals damage.",  "💀", 30, "Dungeon",  660, 18, 14, 26, 18, 36, 60, Element.Dark,     640, 60, 265),
            (bansheeWisp, 0.55m, 1, 2), (mpPotion, 0.30m, 1, 1), (voidStaff, 0.02m, 1, 1));
        Add(Mo("Phantom Banshee", "Passes through walls of stone.", "🌫️", 42, "Abyss",  1065, 25, 20, 34, 26, 50, 84, Element.Dark,    1150, 84, 410),
            (bansheeWisp, 0.60m, 2, 3), (stormBow, 0.04m, 1, 1), (elixir, 0.15m, 1, 1));
        Add(Mo("Queen Banshee",   "Her domain is endless grief.",    "👑", 58, "Abyss",   1830, 38, 28, 44, 30, 70, 116, Element.Dark,   2500, 116, 700),
            (bansheeWisp, 0.70m, 2, 4), (voidStaff, 0.06m, 1, 1), (shadowRobe, 0.05m, 1, 1), (lichDust, 0.20m, 1, 1));

        // OGRES (Mountain Ogre Lv19 exists)
        Add(Mo("Swamp Ogre", "Wallows in mud and misery.",          "👾", 12, "Forest",   265, 10, 8, 4, 4, 14, 24, Element.None,        175, 24,  80),
            (ogreTusk, 0.45m, 1, 2), (wood, 0.40m, 1, 3));
        Add(Mo("Cave Ogre",  "Smashes through stalactites.",        "👾", 26, "Dungeon",  630, 22, 18, 5, 5, 31, 52, Element.None,        525, 52, 220),
            (ogreTusk, 0.50m, 1, 2), (boneCrusher, 0.04m, 1, 1), (hpPotion, 0.25m, 1, 1));
        Add(Mo("Frost Ogre", "Its fists leave bruises of ice.",     "❄️", 34, "Dungeon",  835, 28, 22, 6, 6, 41, 68, Element.Ice,         760, 68, 300),
            (ogreTusk, 0.55m, 2, 3), (frostSword, 0.04m, 1, 1), (boneCrusher, 0.03m, 1, 1));
        Add(Mo("Cyclops",    "One eye, one crushing fist.",         "🔵", 44, "Dungeon", 1165, 36, 28, 8, 7, 53, 88, Element.None,       1260, 88, 430),
            (ogreTusk, 0.60m, 2, 3), (boneCrusher, 0.06m, 1, 1), (steelChest, 0.04m, 1, 1), (largeHpPot, 0.30m, 1, 2));

        if (pending.Count > 0)
        {
            var lootEntries = new List<MonsterLootEntry>();
            foreach (var (m, loot) in pending)
            {
                db.MonsterDefinitions.Add(m);
                foreach (var (item, chance, min, max) in loot)
                {
                    if (item is null) continue;
                    lootEntries.Add(new MonsterLootEntry
                    {
                        MonsterDefinitionId = m.Id,
                        ItemDefinitionId    = item.Id,
                        DropChance          = chance,
                        MinQuantity         = min,
                        MaxQuantity         = max
                    });
                }
            }
            db.MonsterLootEntries.AddRange(lootEntries);
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    private static ItemDefinition W(string name, string desc, string icon, int levelReq,
        int minDmg, int maxDmg,
        int str = 0, int def = 0, int @int = 0, int dex = 0, int vit = 0, int luk = 0,
        long buy = 0, long sell = 0,
        Element element = Element.None,
        ItemSubType subType = ItemSubType.Sword,
        GameItemRarity rarity = GameItemRarity.Rare) => new()
    {
        Name = name, Description = desc, Icon = icon,
        Type = GameItemType.Weapon, SubType = subType, EquipSlot = EquipSlot.MainHand,
        Rarity = rarity, LevelReq = levelReq, MinDamage = minDmg, MaxDamage = maxDmg,
        BonusSTR = str, BonusDEF = def, BonusINT = @int, BonusDEX = dex, BonusVIT = vit, BonusLUK = luk,
        Element = element, BuyPrice = buy, SellPrice = sell
    };

    private static ItemDefinition Ar(string name, string desc, string icon, int levelReq,
        EquipSlot slot, GameItemRarity rarity,
        int def = 0, int str = 0, int @int = 0, int dex = 0, int vit = 0, int luk = 0,
        long buy = 0, long sell = 0) => new()
    {
        Name = name, Description = desc, Icon = icon, Type = GameItemType.Armor,
        SubType = slot switch
        {
            EquipSlot.Head    => ItemSubType.Helmet,
            EquipSlot.Chest   => ItemSubType.Chestplate,
            EquipSlot.Legs    => ItemSubType.Leggings,
            EquipSlot.Feet    => ItemSubType.Boots,
            EquipSlot.OffHand => ItemSubType.Shield,
            _                 => ItemSubType.Helmet
        },
        EquipSlot = slot, Rarity = rarity, LevelReq = levelReq,
        BonusDEF = def, BonusSTR = str, BonusINT = @int, BonusDEX = dex, BonusVIT = vit, BonusLUK = luk,
        BuyPrice = buy, SellPrice = sell
    };

    private static ItemDefinition Co(string name, string desc, string icon, int levelReq,
        int heal = 0, int mana = 0,
        GameItemRarity rarity = GameItemRarity.Uncommon, long buy = 0, long sell = 0) => new()
    {
        Name = name, Description = desc, Icon = icon,
        Type = GameItemType.Consumable,
        SubType = heal > 0 ? ItemSubType.HealthPotion : ItemSubType.ManaPotion,
        Rarity = rarity, LevelReq = levelReq, IsStackable = true,
        HealAmount = heal, ManaRestoreAmount = mana,
        BuyPrice = buy, SellPrice = sell
    };

    private static ItemDefinition Ma(string name, string desc, string icon,
        long buy = 0, long sell = 0, GameItemRarity rarity = GameItemRarity.Common) => new()
    {
        Name = name, Description = desc, Icon = icon,
        Type = GameItemType.Material, SubType = ItemSubType.MonsterDrop,
        Rarity = rarity, LevelReq = 1, IsStackable = true,
        BuyPrice = buy, SellPrice = sell
    };

    private static MonsterDefinition Mo(string name, string desc, string icon, int level, string zone,
        int maxHp, int str, int def, int @int, int dex, int minDmg, int maxDmg,
        Element element, long xpReward, long orbMin, long orbMax) => new()
    {
        Name = name, Description = desc, Icon = icon, Level = level, Zone = zone,
        MaxHp = maxHp, STR = str, DEF = def, INT = @int, DEX = dex,
        MinDamage = minDmg, MaxDamage = maxDmg, Element = element,
        XpReward = xpReward, OrbRewardMin = orbMin, OrbRewardMax = orbMax
    };
}
