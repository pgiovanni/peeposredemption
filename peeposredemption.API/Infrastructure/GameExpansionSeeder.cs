using Microsoft.EntityFrameworkCore;
using peeposredemption.Application.Features.Game.Commands;
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

        // ── DRAGONSCALE ARMOR SET (Lv 60, Epic) ───────────────────────────────
        var dsHelmet = GetOrCreate("Dragonscale Helmet",     () => Ar("Dragonscale Helmet",     "Scales of an ancient dragon.",  "🐲", 60, EquipSlot.Head,  GameItemRarity.Epic, def: 15, vit: 5, str: 3, buy: 6000,  sell: 2000));
        var dsChest  = GetOrCreate("Dragonscale Chestplate", () => Ar("Dragonscale Chestplate", "Nigh-impenetrable plate.",      "🛡️", 60, EquipSlot.Chest, GameItemRarity.Epic, def: 25, vit: 8, str: 5, buy: 10000, sell: 3500));
        var dsLegs   = GetOrCreate("Dragonscale Leggings",   () => Ar("Dragonscale Leggings",   "Heavy fireproof leg plates.",   "🦿", 60, EquipSlot.Legs,  GameItemRarity.Epic, def: 18, vit: 6, str: 4, buy: 7500,  sell: 2500));
        var dsBoots  = GetOrCreate("Dragonscale Boots",      () => Ar("Dragonscale Boots",      "Clawed dragon boots.",          "🥾", 60, EquipSlot.Feet,  GameItemRarity.Epic, def: 10, vit: 4, dex: 2, buy: 4000,  sell: 1400));

        // ── PRIMORDIAL ARMOR SET (Lv 75, Legendary) — drop only, no buy ───────
        var primHelmet = GetOrCreate("Primordial Helmet",     () => Ar("Primordial Helmet",     "Forged in dragonfire itself.",        "🔱", 75, EquipSlot.Head,  GameItemRarity.Legendary, def: 28, vit: 10, str: 6,  @int: 4, buy: 0, sell: 8000));
        var primChest  = GetOrCreate("Primordial Chestplate", () => Ar("Primordial Chestplate", "No blade has ever pierced this.",     "🛡️", 75, EquipSlot.Chest, GameItemRarity.Legendary, def: 45, vit: 15, str: 10, @int: 5, buy: 0, sell: 14000));
        var primLegs   = GetOrCreate("Primordial Leggings",   () => Ar("Primordial Leggings",   "Weighs nothing, stops everything.",   "🦿", 75, EquipSlot.Legs,  GameItemRarity.Legendary, def: 32, vit: 12, str: 7,  dex: 3,  buy: 0, sell: 10000));
        var primBoots  = GetOrCreate("Primordial Boots",      () => Ar("Primordial Boots",      "Leaves scorched footprints.",         "🥾", 75, EquipSlot.Feet,  GameItemRarity.Legendary, def: 20, vit: 8,  str: 4,  dex: 5,  buy: 0, sell: 6000));

        // ── VOID ARMOR SET (Lv 85, Legendary) — drop only, no buy ─────────────
        var voidHelmet = GetOrCreate("Void Helmet",     () => Ar("Void Helmet",     "The abyss gazes back.",               "🌑", 85, EquipSlot.Head,  GameItemRarity.Legendary, def: 35, vit: 12, @int: 10, luk: 5, buy: 0, sell: 12000));
        var voidChest  = GetOrCreate("Void Chestplate", () => Ar("Void Chestplate", "Reality warps around it.",            "🌑", 85, EquipSlot.Chest, GameItemRarity.Legendary, def: 55, vit: 18, @int: 15, luk: 6, buy: 0, sell: 20000));
        var voidLegs   = GetOrCreate("Void Leggings",   () => Ar("Void Leggings",   "Woven from compressed darkness.",     "🌑", 85, EquipSlot.Legs,  GameItemRarity.Legendary, def: 40, vit: 14, @int: 12, luk: 5, buy: 0, sell: 15000));
        var voidBoots  = GetOrCreate("Void Boots",      () => Ar("Void Boots",      "Silent as the void, twice as deadly.","🌑", 85, EquipSlot.Feet,  GameItemRarity.Legendary, def: 25, vit: 10, dex: 8,   luk: 6, buy: 0, sell: 9000));

        // ── LEGENDARY WEAPONS (drop only) ─────────────────────────────────────
        var primordialBlade = GetOrCreate("Primordial Blade",  () => W("Primordial Blade",  "The first sword — older than memory.",        "🔥", 70, 80, 130, str: 18, def: 8,  buy: 0, sell: 20000, element: Element.Fire,  rarity: GameItemRarity.Legendary));
        var voidReaper      = GetOrCreate("Void Reaper",       () => W("Void Reaper",       "Harvests souls with every swing.",            "🌑", 80, 90, 150, str: 15, @int: 20, buy: 0, sell: 28000, element: Element.Void,  rarity: GameItemRarity.Legendary, subType: ItemSubType.Staff));
        var serpentFang     = GetOrCreate("Serpent Fang",      () => W("Serpent Fang",       "A fang the size of a greatsword.",           "🐍", 85, 95, 160, str: 20, dex: 10, buy: 0, sell: 35000, element: Element.Poison,rarity: GameItemRarity.Legendary));

        // ── NEW CONSUMABLES ────────────────────────────────────────────────────
        var largeHpPot = GetOrCreate("Large Health Potion", () => Co("Large Health Potion", "Restores 100 HP.", "🧪", 20, heal: 100, buy: 100, sell: 35));
        var largeMpPot = GetOrCreate("Large Mana Potion",   () => Co("Large Mana Potion",   "Restores 80 MP.",  "💧", 20, mana: 80,  buy: 100, sell: 35));
        var elixir     = GetOrCreate("Elixir of Life",      () => Co("Elixir of Life",      "Restores 200 HP and 150 MP.", "⚗️", 40, heal: 200, mana: 150, rarity: GameItemRarity.Rare, buy: 500, sell: 175));

        // ── GATHERING TOOLS ────────────────────────────────────────────────────
        // BonusLUK = gather bonus (extra qty per action). Intentionally weak weapons.
        // Pickaxes — equip in MainHand for mining bonus
        GetOrCreate("Bronze Pickaxe", () => To("Bronze Pickaxe", "A basic copper-tipped pickaxe. +1 ore per mine.",        "⛏️",  1, ItemSubType.Pickaxe,    gatherBonus: 1,  buy:    50, sell:   15));
        GetOrCreate("Iron Pickaxe",   () => To("Iron Pickaxe",   "Tougher iron head. +2 ore per mine.",                   "⛏️", 10, ItemSubType.Pickaxe,    gatherBonus: 2,  buy:   200, sell:   60));
        GetOrCreate("Steel Pickaxe",  () => To("Steel Pickaxe",  "Well-balanced steel. +3 ore per mine.",                 "⛏️", 20, ItemSubType.Pickaxe,    gatherBonus: 3,  buy:   600, sell:  180));
        GetOrCreate("Mithril Pickaxe",() => To("Mithril Pickaxe","Lightweight mithril head. +5 ore per mine.",            "⛏️", 40, ItemSubType.Pickaxe,    gatherBonus: 5,  buy:  2000, sell:  600, rarity: GameItemRarity.Rare));
        GetOrCreate("Adamantium Pickaxe", () => To("Adamantium Pickaxe", "Effortlessly splits stone. +7 ore per mine.",    "⛏️", 60, ItemSubType.Pickaxe,    gatherBonus: 7,  buy:  6000, sell: 1800, rarity: GameItemRarity.Rare));
        GetOrCreate("Dragon Pickaxe", () => To("Dragon Pickaxe", "Dragonbone head — mines faster than thought. +10 ore.", "⛏️", 80, ItemSubType.Pickaxe,    gatherBonus: 10, buy: 20000, sell: 6000, rarity: GameItemRarity.Epic));

        // Axes — equip in MainHand for woodcutting bonus
        GetOrCreate("Bronze Axe",     () => To("Bronze Axe",     "A crude bronze axe. +1 log per chop.",                  "🪓",  1, ItemSubType.Axe,        gatherBonus: 1,  buy:    40, sell:   12));
        GetOrCreate("Iron Axe",       () => To("Iron Axe",       "A reliable iron axe. +2 logs per chop.",                "🪓", 10, ItemSubType.Axe,        gatherBonus: 2,  buy:   180, sell:   55));
        GetOrCreate("Steel Axe",      () => To("Steel Axe",      "Sharp and sturdy. +3 logs per chop.",                   "🪓", 20, ItemSubType.Axe,        gatherBonus: 3,  buy:   500, sell:  150));
        GetOrCreate("Dragon Axe",     () => To("Dragon Axe",     "Splits ancient trees in one swing. +7 logs per chop.",  "🪓", 60, ItemSubType.Axe,        gatherBonus: 7,  buy:  8000, sell: 2400, rarity: GameItemRarity.Epic));

        // Fishing Rods — equip in MainHand for fishing bonus
        GetOrCreate("Fishing Rod",    () => To("Fishing Rod",    "A basic bamboo rod. +1 fish per cast.",                 "🎣",  1, ItemSubType.FishingRod, gatherBonus: 1,  buy:    30, sell:   10));
        GetOrCreate("Fly Rod",        () => To("Fly Rod",        "A finely crafted fly rod. +2 fish per cast.",           "🎣", 15, ItemSubType.FishingRod, gatherBonus: 2,  buy:   150, sell:   45));
        GetOrCreate("Crystal Rod",    () => To("Crystal Rod",    "Crystal tip attracts rare fish. +4 fish per cast.",     "🎣", 35, ItemSubType.FishingRod, gatherBonus: 4,  buy:   800, sell:  240, rarity: GameItemRarity.Rare));
        GetOrCreate("Abyssal Rod",    () => To("Abyssal Rod",    "Reaches the deepest trenches. +7 fish per cast.",       "🎣", 65, ItemSubType.FishingRod, gatherBonus: 7,  buy:  4000, sell: 1200, rarity: GameItemRarity.Rare));

        // ── COOKED FOOD (Cooking skill) ────────────────────────────────────────
        GetOrCreate("Cooked Shrimp",      () => Co("Cooked Shrimp",      "Freshly cooked shrimp — restores 3 HP.",         "🍤",  1, heal:  3,        buy:   4, sell:  2));
        GetOrCreate("Cooked Trout",       () => Co("Cooked Trout",       "A nicely grilled trout — restores 7 HP.",        "🐟",  1, heal:  7,        buy:  10, sell:  5));
        GetOrCreate("Cooked Salmon",      () => Co("Cooked Salmon",      "Perfectly cooked salmon — restores 12 HP.",      "🐠",  1, heal: 12,        buy:  18, sell:  8));
        GetOrCreate("Cooked Tuna",        () => Co("Cooked Tuna",        "A hearty cooked tuna — restores 18 HP.",         "🐡",  1, heal: 18,        buy:  28, sell: 12));
        GetOrCreate("Cooked Lobster",     () => Co("Cooked Lobster",     "Succulent cooked lobster — restores 25 HP.",     "🦞",  1, heal: 25,        buy:  40, sell: 18));
        GetOrCreate("Cooked Swordfish",   () => Co("Cooked Swordfish",   "A powerful swordfish steak — restores 35 HP.",   "🐟",  1, heal: 35,        buy:  55, sell: 25));
        GetOrCreate("Cooked Shark",       () => Co("Cooked Shark",       "Thick shark meat — restores 50 HP.",             "🦈",  1, heal: 50,        buy:  80, sell: 38));
        GetOrCreate("Cooked Abyssal Eel", () => Co("Cooked Abyssal Eel", "Mystical eel — restores 70 HP and 20 MP.",       "🌑",  1, heal: 70, mana: 20, buy: 140, sell: 60, rarity: GameItemRarity.Rare));
        GetOrCreate("Burnt Fish",         () => Fo("Burnt Fish",         "You burnt it. Completely inedible.",             "🖤",  buy: 2, sell: 1));
        // TODO: Fish Stew is intended to be a 15 HP/turn HoT over 3 turns (45 HP total).
        // Currently grants 20 HP instantly. Full HoT requires RegenHpPerTurn + RegenTurnsRemaining on CombatSession.
        GetOrCreate("Fish Stew",          () => Co("Fish Stew",          "A rich stew — restores 20 HP in combat.",        "🍲",  1, heal: 20,        buy:  60, sell: 30, rarity: GameItemRarity.Uncommon));

        // ── SMELTED BARS (Smithing output) ────────────────────────────────────
        GetOrCreate("Copper Bar",    () => Ma("Copper Bar",    "Smelted copper.",              "🟫", buy:    10, sell:    4));
        GetOrCreate("Iron Bar",      () => Ma("Iron Bar",      "Smelted iron.",                "⚙️", buy:    20, sell:    8));
        GetOrCreate("Silver Bar",    () => Ma("Silver Bar",    "Refined silver.",              "⚪", buy:    60, sell:   22));
        GetOrCreate("Gold Bar",      () => Ma("Gold Bar",      "Pure gold.",                   "🟡", buy:   140, sell:   52));
        GetOrCreate("Mithril Bar",   () => Ma("Mithril Bar",   "Lightweight and strong.",      "🔵", buy:   320, sell:  115, rarity: GameItemRarity.Uncommon));
        GetOrCreate("Adamantite Bar",() => Ma("Adamantite Bar","Dense, near-unbreakable.",     "🟢", buy:   700, sell:  255, rarity: GameItemRarity.Uncommon));
        GetOrCreate("Adamantium Bar",() => Ma("Adamantium Bar","Legendary ore, refined.",      "🔷", buy:  1500, sell:  550, rarity: GameItemRarity.Rare));
        GetOrCreate("Void Ingot",    () => Ma("Void Ingot",    "Crystallised void energy.",    "🌑", buy:  3500, sell: 1300, rarity: GameItemRarity.Rare));

        // ── MITHRIL ARMOR SET (Lv 40, Rare — craft only) ──────────────────────
        var mithHelmet = GetOrCreate("Mithril Helmet",     () => Ar("Mithril Helmet",     "Feather-light yet surprisingly tough.", "🪖", 40, EquipSlot.Head,  GameItemRarity.Rare, def: 12, vit: 3, str: 2, buy: 0, sell:  900));
        var mithChest  = GetOrCreate("Mithril Chestplate", () => Ar("Mithril Chestplate", "Mithril rings woven into plate.",       "🛡️", 40, EquipSlot.Chest, GameItemRarity.Rare, def: 20, vit: 5, str: 3, buy: 0, sell: 1600));
        var mithLegs   = GetOrCreate("Mithril Leggings",   () => Ar("Mithril Leggings",   "Offers surprising mobility.",           "🦿", 40, EquipSlot.Legs,  GameItemRarity.Rare, def: 15, vit: 4, str: 2, buy: 0, sell: 1200));
        var mithBoots  = GetOrCreate("Mithril Boots",      () => Ar("Mithril Boots",      "Silent and sturdy.",                    "🥾", 40, EquipSlot.Feet,  GameItemRarity.Rare, def:  8, vit: 2, dex: 2, buy: 0, sell:  650));

        // ── ADAMANTITE ARMOR SET (Lv 55, Epic — craft only) ───────────────────
        var adamHelmet = GetOrCreate("Adamantite Helmet",     () => Ar("Adamantite Helmet",     "Practically indestructible.",    "🪖", 55, EquipSlot.Head,  GameItemRarity.Epic, def: 18, vit: 6, str: 4, buy: 0, sell: 2500));
        var adamChest  = GetOrCreate("Adamantite Chestplate", () => Ar("Adamantite Chestplate", "Almost no weapon can pierce it.","🛡️", 55, EquipSlot.Chest, GameItemRarity.Epic, def: 30, vit: 9, str: 6, buy: 0, sell: 4200));
        var adamLegs   = GetOrCreate("Adamantite Leggings",   () => Ar("Adamantite Leggings",   "Heavy but worth every pound.",   "🦿", 55, EquipSlot.Legs,  GameItemRarity.Epic, def: 22, vit: 7, str: 5, buy: 0, sell: 3000));
        var adamBoots  = GetOrCreate("Adamantite Boots",      () => Ar("Adamantite Boots",      "Dense boots with solid grip.",   "🥾", 55, EquipSlot.Feet,  GameItemRarity.Epic, def: 14, vit: 4, dex: 3, buy: 0, sell: 1700));

        // ── SMITHABLE WEAPONS (craft only) ────────────────────────────────────
        GetOrCreate("Mithril Blade",     () => W("Mithril Blade",     "Unnervingly sharp blue-grey blade.",      "🔵", 35, 30, 48, str: 8,        buy: 0, sell:  1400, rarity: GameItemRarity.Rare));
        GetOrCreate("Adamantite Sword",  () => W("Adamantite Sword",  "Cleaves through light armour.",           "🟢", 50, 40, 62, str: 12, def: 3, buy: 0, sell:  3000, rarity: GameItemRarity.Rare));
        GetOrCreate("Adamantium Blade",  () => W("Adamantium Blade",  "The finest non-legendary blade.",         "🔷", 65, 60, 95, str: 18, def: 5, buy: 0, sell:  9000, rarity: GameItemRarity.Epic));

        // ── WOODCRAFT WEAPONS (craft only) ────────────────────────────────────
        GetOrCreate("Oak Staff",         () => W("Oak Staff",         "Sturdy oak wand for novice mages.",       "🪵", 15,  8, 14, @int:  5, buy: 0, sell:   120, subType: ItemSubType.Staff));
        GetOrCreate("Willow Shortbow",   () => W("Willow Shortbow",   "Flexible willow, decent range.",          "🏹", 30, 20, 32, dex:   5, buy: 0, sell:   400, subType: ItemSubType.Bow, rarity: GameItemRarity.Uncommon));
        GetOrCreate("Maple Longbow",     () => W("Maple Longbow",     "Draws harder, hits further.",             "🏹", 45, 28, 44, dex:   7, buy: 0, sell:  1000, subType: ItemSubType.Bow, rarity: GameItemRarity.Rare));
        GetOrCreate("Yew Longbow",       () => W("Yew Longbow",       "Prized by seasoned archers.",             "🏹", 60, 38, 60, dex:  10, buy: 0, sell:  2500, subType: ItemSubType.Bow, rarity: GameItemRarity.Rare));
        GetOrCreate("Magic Staff",       () => W("Magic Staff",       "Carved from magic-infused heartwood.",    "✨", 75, 50, 80, @int: 15, buy: 0, sell:  7000, subType: ItemSubType.Staff, rarity: GameItemRarity.Epic));
        GetOrCreate("Void Wood Staff",   () => W("Void Wood Staff",   "Resonates with the void itself.",         "🌑", 90, 70,110, @int: 20, buy: 0, sell: 14000, subType: ItemSubType.Staff, element: Element.Void, rarity: GameItemRarity.Epic));

        // ── GATHERING RESOURCES — MINING ──────────────────────────────────────
        var silverOre      = GetOrCreate("Silver Ore",      () => Ma("Silver Ore",      "Shiny silver ore.",                     "⚪", buy:   50, sell:   20));
        var goldOre        = GetOrCreate("Gold Ore",        () => Ma("Gold Ore",        "Glittering gold ore.",                  "🟡", buy:  120, sell:   45));
        var mithrilOre     = GetOrCreate("Mithril Ore",     () => Ma("Mithril Ore",     "Lightweight yet incredibly strong.",    "🔵", buy:  280, sell:  100));
        var adamantiteOre  = GetOrCreate("Adamantite Ore",  () => Ma("Adamantite Ore",  "One of the hardest metals known.",      "🟢", buy:  600, sell:  220));
        var runiteOre      = GetOrCreate("Adamantium Ore",  () => Ma("Adamantium Ore",  "Dense and near-indestructible ore.",    "🔷", buy: 1300, sell:  480));
        var voidstone      = GetOrCreate("Voidstone",       () => Ma("Voidstone",       "Pulsates with void energy.",            "🌑", buy: 2800, sell: 1000));
        var sapphire       = GetOrCreate("Sapphire",        () => Ma("Sapphire",        "A brilliant blue gemstone.",            "💎", buy:  100, sell:   35));
        var emerald        = GetOrCreate("Emerald",         () => Ma("Emerald",         "A deep green gemstone.",                "💚", buy:  220, sell:   80));
        var ruby           = GetOrCreate("Ruby",            () => Ma("Ruby",            "A fiery red gemstone.",                 "❤️", buy:  480, sell:  175));
        var diamond        = GetOrCreate("Diamond",         () => Ma("Diamond",         "The hardest and rarest gemstone.",      "💠", buy: 1100, sell:  400));

        // ── GATHERING RESOURCES — FISHING ─────────────────────────────────────
        var rawShrimp      = GetOrCreate("Raw Shrimp",      () => Ma("Raw Shrimp",      "Tiny but edible shrimp.",               "🦐", buy:   10, sell:    4));
        var rawTrout       = GetOrCreate("Raw Trout",       () => Ma("Raw Trout",       "A freshwater trout.",                   "🐟", buy:   30, sell:   12));
        var rawSalmon      = GetOrCreate("Raw Salmon",      () => Ma("Raw Salmon",      "A pink-fleshed salmon.",                "🐠", buy:   70, sell:   28));
        var rawTuna        = GetOrCreate("Raw Tuna",        () => Ma("Raw Tuna",        "A large ocean tuna.",                   "🐡", buy:  160, sell:   60));
        var rawLobster     = GetOrCreate("Raw Lobster",     () => Ma("Raw Lobster",     "A hard-shelled lobster.",               "🦞", buy:  350, sell:  130));
        var rawSwordfish   = GetOrCreate("Raw Swordfish",   () => Ma("Raw Swordfish",   "A powerful, sharp-billed fish.",        "🐟", buy:  750, sell:  280));
        var rawShark       = GetOrCreate("Raw Shark",       () => Ma("Raw Shark",       "A formidable ocean predator.",          "🦈", buy: 1600, sell:  580));
        var rawAbyssalEel  = GetOrCreate("Raw Abyssal Eel", () => Ma("Raw Abyssal Eel", "An eel from the deepest trenches.",     "🌑", buy: 3300, sell: 1200));

        // ── GATHERING RESOURCES — WOODCUTTING ─────────────────────────────────
        var oakLogs        = GetOrCreate("Oak Logs",        () => Ma("Oak Logs",        "Sturdy oak wood.",                      "🪵", buy:   30, sell:   12));
        var willowLogs     = GetOrCreate("Willow Logs",     () => Ma("Willow Logs",     "Flexible willow branches.",             "🪵", buy:   75, sell:   28));
        var mapleLogs      = GetOrCreate("Maple Logs",      () => Ma("Maple Logs",      "Dense maple wood.",                     "🪵", buy:  175, sell:   65));
        var yewLogs        = GetOrCreate("Yew Logs",        () => Ma("Yew Logs",        "Ancient yew wood.",                     "🪵", buy:  400, sell:  150));
        var magicLogs      = GetOrCreate("Magic Logs",      () => Ma("Magic Logs",      "Infused with residual magic.",          "✨", buy:  900, sell:  340));
        var voidWood       = GetOrCreate("Void Wood",       () => Ma("Void Wood",       "Wood twisted by void energy.",          "🌑", buy: 2000, sell:  750));

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

        // ── MONSTER DROP PEEPOS ────────────────────────────────────────────────
        // These are drop-only peepos; BuyPrice = 0 (not in coin shop)
        // SellPrice = 50% of the equivalent rarity coin price
        var slimePeepo    = GetOrCreate("Slime Peepo",    () => Pe("Slime Peepo",    "A squishy peepo covered in green goo.",      "🟢", GameItemRarity.Common,    sellPrice: 25));
        var dragonPeepo   = GetOrCreate("Dragon Peepo",   () => Pe("Dragon Peepo",   "A fierce peepo wreathed in dragon fire.",    "🐉", GameItemRarity.Rare,      sellPrice: 250));
        var demonPeepo    = GetOrCreate("Demon Peepo",    () => Pe("Demon Peepo",    "A sinister peepo with tiny demon horns.",    "👹", GameItemRarity.Epic,      sellPrice: 750));
        var lichPeepo     = GetOrCreate("Lich Peepo",     () => Pe("Lich Peepo",     "A skeletal peepo radiating dark magic.",     "💀", GameItemRarity.Rare,      sellPrice: 250));
        var vampirePeepo  = GetOrCreate("Vampire Peepo",  () => Pe("Vampire Peepo",  "A pale peepo with a sweeping black cape.",   "🧛", GameItemRarity.Uncommon,  sellPrice: 75));
        var werewolfPeepo = GetOrCreate("Werewolf Peepo", () => Pe("Werewolf Peepo", "A peepo that howls at the blood moon.",      "🐺", GameItemRarity.Uncommon,  sellPrice: 75));

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

        // SLIMES (Slime Lv1 exists — Easy tier)
        // Lv 1 band: Easy = Slime (base seeder), Normal = Goopling, Hard = Corrosive Ooze, Very Hard = Chaos Jelly
        Add(Mo("Goopling",        "A slightly feisty blob with attitude.",      "🟩",  1, "Plains",    38,  2, 1,  1, 2,  2,  4, Element.None,        22,  4,  15),
            (slimeCore, 0.30m, 1, 1));
        Add(Mo("Corrosive Ooze",  "Acidic slime that eats through metal.",      "🟨",  1, "Plains",    70,  3, 1,  3, 2,  3,  6, Element.Poison,       50,  8,  28),
            (slimeCore, 0.55m, 1, 1), (smallHpPotion, 0.15m, 1, 1));
        Add(Mo("Chaos Jelly",     "Unpredictably violent blob of pure mayhem.", "🌀",  1, "Plains",   110,  4, 1,  4, 3,  5,  9, Element.Dark,          85, 12,  45),
            (slimeCore, 0.65m, 1, 2), (smallMpPotion, 0.20m, 1, 1));

        // Lv 4 band: Easy = Blue Slime, Normal = Tar Glob, Hard = Plague Blot, Very Hard = Void Bubble
        Add(Mo("Blue Slime",    "A cool, ice-tinged blob.",         "🔵",  4, "Plains",    84,  3, 2,  3, 2,  5,  8, Element.Ice,         60,  8,  30),
            (slimeCore, 0.50m, 1, 1), (smallMpPotion, 0.25m, 1, 1));
        Add(Mo("Tar Glob",      "Sticky, slow, and surprisingly tough.",        "🟤",  4, "Plains",   140,  4, 3,  2, 1,  6, 10, Element.None,        100, 12,  42),
            (slimeCore, 0.55m, 1, 2), (copperOre, 0.25m, 1, 1));
        Add(Mo("Plague Blot",   "Pestilent ooze that spreads disease.",         "🟫",  4, "Plains",   200,  5, 2,  5, 2,  7, 12, Element.Poison,       155, 16,  60),
            (slimeCore, 0.60m, 1, 2), (smallHpPotion, 0.20m, 1, 1));
        Add(Mo("Void Bubble",   "A sphere of nothingness that devours light.",  "⚫",  4, "Plains",   280,  6, 2,  7, 3,  9, 15, Element.Void,          225, 22,  80),
            (slimeCore, 0.65m, 1, 2), (smallMpPotion, 0.30m, 1, 1));

        Add(Mo("Red Slime",     "Burns on contact.",                "🔴",  8, "Forest",   156,  5, 4,  6, 3, 10, 16, Element.Fire,        110, 16,  55),
            (slimeCore, 0.55m, 1, 2), (smallHpPotion, 0.20m, 1, 1));
        Add(Mo("Purple Slime",  "Oozes dark toxins.",              "🟣", 13, "Forest",   246,  8, 7,  9, 5, 16, 26, Element.Dark,        195, 26,  90),
            (slimeCore, 0.60m, 1, 2), (smallHpPotion, 0.15m, 1, 1));
        Add(Mo("Crystal Slime", "Crackles with electricity.",      "💎", 20, "Mountains", 460, 13, 10, 14, 9, 24, 40, Element.Lightning,  390, 40, 170),
            (slimeCore, 0.50m, 1, 3), (smallMpPotion, 0.30m, 1, 1));
        Add(Mo("Slime King",    "An enormous, regal blob.",        "👑", 35, "Dungeon",   920, 20, 18, 18, 10, 42, 70, Element.Earth,      820, 70, 320),
            (slimeCore, 0.90m, 2, 5), (largeHpPot, 0.30m, 1, 1), (steelSword, 0.04m, 1, 1));

        // GOBLINS (Goblin Lv3 exists — Normal tier)
        // Lv 3 band: Easy = Scrappy Runt, Hard = Spear Goblin, Very Hard = Darkspawn Imp
        Add(Mo("Scrappy Runt",    "Barely armed, mostly just annoying.",        "👶",  3, "Plains",    52,  2, 1,  1, 4,  3,  5, Element.None,         32,  5,  18),
            (goblinEar, 0.35m, 1, 1));
        Add(Mo("Spear Goblin",    "Throws crude spears with surprising aim.",   "🏹",  3, "Plains",   110,  4, 2,  2, 6,  5,  9, Element.None,          80, 10,  38),
            (goblinEar, 0.55m, 1, 2), (copperOre, 0.20m, 1, 1));
        Add(Mo("Darkspawn Imp",   "Tiny demon-goblin hybrid with vile magic.", "😈",  3, "Plains",   155,  5, 2,  5, 5,  7, 11, Element.Dark,          120, 14,  52),
            (goblinEar, 0.60m, 1, 2), (smallMpPotion, 0.20m, 1, 1));

        Add(Mo("Cave Goblin",     "Sneaks through the darkness.",    "👺",  6, "Forest",   120,  4, 3,  4, 5,  7, 12, Element.None,         80, 10,  40),
            (goblinEar, 0.55m, 1, 2), (copperOre, 0.25m, 1, 2));
        Add(Mo("Hobgoblin",       "Bigger, meaner cousin.",          "👺", 11, "Mountains", 220, 10, 7,  5, 6, 13, 22, Element.None,        165, 22,  80),
            (goblinEar, 0.50m, 1, 2), (ironOre, 0.30m, 1, 2), (ironSword, 0.04m, 1, 1));
        Add(Mo("Goblin Shaman",   "Calls on dark spirits.",          "🧿", 17, "Dungeon",  315, 10, 9, 15, 7, 20, 34, Element.Dark,         275, 34, 125),
            (goblinEar, 0.40m, 1, 2), (smallMpPotion, 0.35m, 1, 2), (shadowDagger, 0.03m, 1, 1));
        Add(Mo("Goblin Warchief", "Commands entire goblin armies.", "👿", 26, "Dungeon",  590, 22, 15, 10, 14, 31, 52, Element.None,        520, 52, 220),
            (goblinEar, 0.60m, 2, 4), (steelSword, 0.05m, 1, 1), (hpPotion, 0.30m, 1, 2));

        // Lv 6 band extra tiers: Easy = Meadow Sprite, Hard = Venomfang, Very Hard = Gloomhunter
        Add(Mo("Meadow Sprite",   "A gentle forest spirit gone slightly feral.",  "🧚",  6, "Forest",    88,  3, 2,  4, 6,  5,  8, Element.Wind,          55,  8,  30),
            (witchHerb, 0.40m, 1, 1));
        Add(Mo("Venomfang",       "A giant centipede with paralysing venom.",     "🐛",  6, "Forest",   180,  5, 3,  3, 5,  8, 13, Element.Poison,        130, 14,  52),
            (witchHerb, 0.40m, 1, 1), (smallHpPotion, 0.20m, 1, 1));
        Add(Mo("Gloomhunter",     "A shadowy beast that stalks from above.",      "🦅",  6, "Forest",   240,  6, 3,  4, 8, 10, 16, Element.Dark,           190, 18,  68),
            (leather, 0.50m, 1, 2), (smallMpPotion, 0.20m, 1, 1));

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

        // ── ENDGAME MONSTERS (Lv 65-80) ───────────────────────────────────────
        // Fill the gap between Lv62 regulars and Lv80+ bosses

        // ABYSSAL HORRORS
        Add(Mo("Abyssal Horror",    "A creature that shouldn't exist.",     "🌑", 65, "Abyss", 2400, 55, 44, 50, 32, 85, 140, Element.Dark,    4200, 140, 950),
            (voidstone, 0.30m, 1, 2), (lichDust, 0.20m, 1, 1), (demonHorn, 0.60m, 2, 4));
        Add(Mo("Void Stalker",      "Slips between shadows without a sound.","👁️", 68, "Abyss", 2750, 58, 46, 55, 40, 92, 154, Element.Void,  5000, 154, 1050),
            (voidstone, 0.40m, 1, 3), (lichDust, 0.25m, 1, 2), (voidStaff, 0.03m, 1, 1));
        Add(Mo("Chaos Rift",        "A tear in reality given teeth.",        "🌀", 72, "Abyss", 3200, 62, 50, 60, 38, 100, 170, Element.Void,  6000, 170, 1200),
            (voidstone, 0.50m, 1, 3), (dragonClaw, 0.20m, 1, 2), (voidStaff, 0.04m, 1, 1));
        Add(Mo("Dread Colossus",    "Its footsteps collapse caverns.",       "🗿", 75, "Abyss", 4000, 70, 60, 45, 30, 115, 190, Element.Earth, 7500, 190, 1450),
            (voidstone, 0.55m, 2, 4), (golemCore, 0.40m, 2, 3), (primChest, 0.02m, 1, 1));
        Add(Mo("Eternal Wyvern",    "Has outlived civilizations.",           "🐉", 70, "Abyss", 3000, 65, 52, 48, 35, 105, 175, Element.Fire,   5500, 175, 1100),
            (dragonScale, 0.70m, 3, 5), (dragonClaw, 0.40m, 1, 3), (primordialBlade, 0.01m, 1, 1));
        Add(Mo("Shadow Colossus",   "Born from the darkness between stars.", "🖤", 78, "Abyss", 4800, 72, 58, 68, 40, 130, 215, Element.Dark,  9000, 215, 1700),
            (voidstone, 0.60m, 2, 5), (lichDust, 0.40m, 2, 3), (voidReaper, 0.01m, 1, 1));

        // ── BOSSES ─────────────────────────────────────────────────────────────
        // Boss HP is 5–10x a regular monster at the same level.
        // Players must use /rpg boss <name> to target them — they won't appear in random fights.
        Add(Mo("King Slime Prime",   "An enormous, crowned slime that shakes the earth.",   "👑", 15, "Plains",   3500,  25, 20, 15, 12, 60, 200, Element.Earth,  8000, 800,  2000),
            (slimeCore, 0.80m, 3, 5), (largeHpPot, 0.50m, 1, 2), (largeMpPot, 0.50m, 1, 2));
        Add(Mo("Goblin Overlord",    "Commands goblin armies with an iron fist.",            "👺", 20, "Forest",   5000,  35, 25, 20, 18, 80, 260, Element.None,   12000, 1200, 3000),
            (goblinEar, 0.80m, 4, 6), (steelSword, 0.30m, 1, 1), (largeHpPot, 0.50m, 1, 3));
        Add(Mo("Elder Forest Troll", "Ancient beyond reckoning — regenerates rapidly.",     "🧌", 25, "Forest",   7000,  45, 38, 10,  8, 100, 320, Element.Earth, 18000, 1800, 4500),
            (trollHide, 0.80m, 3, 5), (frostSword, 0.20m, 1, 1), (largeHpPot, 0.60m, 2, 3));
        Add(Mo("Dungeon Warden",     "An undead guardian that never tires, never yields.",  "💀", 30, "Dungeon",  9000,  50, 42, 30, 20, 120, 380, Element.Dark,  25000, 2500, 6000),
            (skullFragment, 0.80m, 3, 5), (voidStaff, 0.15m, 1, 1), (shadowRobe, 0.15m, 1, 1), (elixir, 0.60m, 1, 2));
        Add(Mo("Volcanic Titan",     "A mountain given form, burning with inner fire.",     "🌋", 40, "Volcano", 15000,  65, 55, 25, 15, 160, 500, Element.Fire,  40000, 4000, 10000),
            (golemCore, 0.80m, 3, 5), (obsidianBlade, 0.25m, 1, 1), (dsChest, 0.10m, 1, 1), (elixir, 0.70m, 1, 3));
        Add(Mo("Abyssal Overlord",   "Its very presence erases light from existence.",      "🌑", 50, "Abyss",   25000,  80, 65, 60, 35, 220, 680, Element.Dark,  65000, 6500, 16000),
            (demonHorn, 0.80m, 4, 6), (voidStaff, 0.25m, 1, 1), (dsChest, 0.05m, 1, 1), (elixir, 0.80m, 2, 3));
        Add(Mo("Lich King",          "Death incarnate — commands all undead in the abyss.", "💀", 60, "Abyss",   35000,  75, 60, 85, 30, 280, 860, Element.Dark,  90000, 9000, 22000),
            (lichDust, 0.90m, 4, 8), (voidStaff, 0.30m, 1, 1), (holyLance, 0.15m, 1, 1), (voidReaper, 0.02m, 1, 1), (elixir, 0.90m, 2, 4));
        Add(Mo("Primordial Dragon",  "The first dragon — reality warps in its wake.",       "🐉", 80, "Abyss",   60000, 100, 85, 75, 50, 400, 1200, Element.Fire, 150000, 15000, 40000),
            (dragonScale, 0.90m, 5, 10), (dsChest, 0.06m, 1, 1), (dsHelmet, 0.06m, 1, 1), (primordialBlade, 0.03m, 1, 1), (primChest, 0.04m, 1, 1), (primHelmet, 0.04m, 1, 1), (elixir, 1.00m, 3, 5));
        Add(Mo("World Serpent",      "The omega of all things. To fight it is madness.",   "🐍", 90, "Abyss",  100000, 120, 100, 90, 70, 550, 1650, Element.Poison, 250000, 25000, 65000),
            (voidstone, 0.90m, 5, 10), (dragonScale, 0.50m, 3, 6), (elixir, 1.00m, 5, 8), (serpentFang, 0.04m, 1, 1), (voidChest, 0.03m, 1, 1), (primChest, 0.06m, 1, 1));

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

        // ── PEEPO MONSTER LOOT ENTRIES ─────────────────────────────────────────
        // Add peepo drops to existing monsters (idempotent — check by monster + item before inserting)
        var peepoDrops = new[]
        {
            ("Slime King",           slimePeepo,    0.15m),
            ("Ancient Dragon",       dragonPeepo,   0.05m),
            ("Archdemon",            demonPeepo,    0.03m),
            ("Lich",                 lichPeepo,     0.04m),
            ("Ancient Vampire",      vampirePeepo,  0.08m),
            ("Blood Moon Werewolf",  werewolfPeepo, 0.06m),
        };

        var existingLootPairs = await db.MonsterLootEntries
            .Select(e => new { e.MonsterDefinitionId, e.ItemDefinitionId })
            .ToHashSetAsync(ct);

        var newPeepoLoot = new List<MonsterLootEntry>();
        foreach (var (monsterName, peepoItem, chance) in peepoDrops)
        {
            if (peepoItem is null) continue;
            var monster = await db.MonsterDefinitions.FirstOrDefaultAsync(m => m.Name == monsterName, ct);
            if (monster is null) continue;
            var key = new { MonsterDefinitionId = monster.Id, ItemDefinitionId = peepoItem.Id };
            if (existingLootPairs.Any(p => p.MonsterDefinitionId == key.MonsterDefinitionId && p.ItemDefinitionId == key.ItemDefinitionId))
                continue;
            newPeepoLoot.Add(new MonsterLootEntry
            {
                MonsterDefinitionId = monster.Id,
                ItemDefinitionId    = peepoItem.Id,
                DropChance          = chance,
                MinQuantity         = 1,
                MaxQuantity         = 1
            });
        }
        if (newPeepoLoot.Count > 0)
        {
            db.MonsterLootEntries.AddRange(newPeepoLoot);
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

    /// <summary>Zero-effect food item (e.g. Burnt Fish). Consumable type so it shows in the item slot.</summary>
    private static ItemDefinition Fo(string name, string desc, string icon,
        long buy = 0, long sell = 0) => new()
    {
        Name = name, Description = desc, Icon = icon,
        Type = GameItemType.Consumable, SubType = ItemSubType.HealthPotion,
        Rarity = GameItemRarity.Common, LevelReq = 1, IsStackable = true,
        HealAmount = 0, ManaRestoreAmount = 0,
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
        Element element, long xpReward, long orbMin, long orbMax,
        string? abilities = null) => new()
    {
        Name = name, Description = desc, Icon = icon, Level = level, Zone = zone,
        MaxHp = maxHp, STR = str, DEF = def, INT = @int, DEX = dex,
        MinDamage = minDmg, MaxDamage = maxDmg, Element = element,
        XpReward = xpReward, OrbRewardMin = orbMin, OrbRewardMax = orbMax,
        AbilityJson = abilities
    };

    /// <summary>Gathering tool factory. BonusLUK = extra qty per gather action.</summary>
    private static ItemDefinition To(string name, string desc, string icon, int levelReq,
        ItemSubType subType, int gatherBonus,
        long buy = 0, long sell = 0,
        GameItemRarity rarity = GameItemRarity.Uncommon) => new()
    {
        Name = name, Description = desc, Icon = icon,
        Type = GameItemType.Weapon, SubType = subType, EquipSlot = EquipSlot.MainHand,
        Rarity = rarity, LevelReq = levelReq,
        MinDamage = 1, MaxDamage = gatherBonus + 1,
        BonusLUK = gatherBonus,
        BuyPrice = buy, SellPrice = sell
    };

    // Peepo collectible factory — drop-only (BuyPrice = 0)
    private static ItemDefinition Pe(string name, string desc, string icon,
        GameItemRarity rarity, long sellPrice = 0) => new()
    {
        Name        = name,
        Description = desc,
        Icon        = icon,
        Type        = GameItemType.Collectible,
        SubType     = ItemSubType.Peepo,
        Rarity      = rarity,
        IsStackable = false,
        BuyPrice    = 0,
        SellPrice   = sellPrice
    };
}
