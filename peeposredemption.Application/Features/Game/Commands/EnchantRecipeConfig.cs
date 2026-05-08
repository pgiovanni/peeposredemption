using peeposredemption.Domain.Entities;

namespace peeposredemption.Application.Features.Game.Commands;

/// <summary>A single enchant book crafting recipe (static, no DB).</summary>
public record EnchantRecipe(
    string BookName,
    Element Element,
    int Tier,
    (string ItemName, int Qty)[] Ingredients);

/// <summary>
/// Static list of all enchant book recipes.
/// Materials are seeded by SeedGameDataCommand.PatchEnchantMaterialsAsync().
/// Tier N books require 2× Tier N-1 books + extra materials.
/// </summary>
public static class EnchantRecipeConfig
{
    public static readonly IReadOnlyList<EnchantRecipe> Recipes = new[]
    {
        // ── Tier 1 — basic elemental essence ───────────────────────────────
        new EnchantRecipe("Blazing Tome I",    Element.Fire,      1, new[]{ ("Ember Shard",    3), ("Fire Essence",     1) }),
        new EnchantRecipe("Glacial Tome I",    Element.Ice,       1, new[]{ ("Frost Crystal",  3), ("Ice Essence",      1) }),
        new EnchantRecipe("Storm Tome I",      Element.Lightning, 1, new[]{ ("Storm Essence",  3), ("Lightning Core",   1) }),
        new EnchantRecipe("Earthen Tome I",    Element.Earth,     1, new[]{ ("Stone Fragment", 3), ("Earth Essence",    1) }),
        new EnchantRecipe("Shadow Tome I",     Element.Dark,      1, new[]{ ("Dark Essence",   3), ("Shadow Core",      1) }),
        new EnchantRecipe("Radiant Tome I",    Element.Holy,      1, new[]{ ("Holy Dust",      3), ("Light Essence",    1) }),
        new EnchantRecipe("Void Codex I",      Element.Void,      1, new[]{ ("Void Fragment",  3), ("Dark Essence",     2), ("Abyssal Core", 1) }),

        // ── Tier 2 — refined with previous tier ────────────────────────────
        new EnchantRecipe("Blazing Tome II",   Element.Fire,      2, new[]{ ("Blazing Tome I",  2), ("Ember Shard",    5), ("Magma Core",   2) }),
        new EnchantRecipe("Glacial Tome II",   Element.Ice,       2, new[]{ ("Glacial Tome I",  2), ("Frost Crystal",  5), ("Ice Core",     2) }),
        new EnchantRecipe("Storm Tome II",     Element.Lightning, 2, new[]{ ("Storm Tome I",    2), ("Storm Essence",  5), ("Thunder Core", 2) }),
        new EnchantRecipe("Earthen Tome II",   Element.Earth,     2, new[]{ ("Earthen Tome I",  2), ("Stone Fragment", 5), ("Earth Core",   2) }),
        new EnchantRecipe("Shadow Tome II",    Element.Dark,      2, new[]{ ("Shadow Tome I",   2), ("Dark Essence",   5), ("Void Shard",   2) }),
        new EnchantRecipe("Radiant Tome II",   Element.Holy,      2, new[]{ ("Radiant Tome I",  2), ("Holy Dust",      5), ("Sacred Ash",   2) }),
        new EnchantRecipe("Void Codex II",     Element.Void,      2, new[]{ ("Void Codex I",    2), ("Void Fragment",  5), ("Dark Essence", 3), ("Abyssal Core", 2) }),

        // ── Tier 3 — rare boss materials required ──────────────────────────
        new EnchantRecipe("Blazing Tome III",  Element.Fire,      3, new[]{ ("Blazing Tome II",  2), ("Ember Shard",    8), ("Magma Core",   4), ("Dragon Scale",   1) }),
        new EnchantRecipe("Glacial Tome III",  Element.Ice,       3, new[]{ ("Glacial Tome II",  2), ("Frost Crystal",  8), ("Ice Core",     4), ("Dragon Scale",   1) }),
        new EnchantRecipe("Storm Tome III",    Element.Lightning, 3, new[]{ ("Storm Tome II",    2), ("Storm Essence",  8), ("Thunder Core", 4), ("Dragon Scale",   1) }),
        new EnchantRecipe("Earthen Tome III",  Element.Earth,     3, new[]{ ("Earthen Tome II",  2), ("Stone Fragment", 8), ("Earth Core",   4), ("Dragon Scale",   1) }),
        new EnchantRecipe("Shadow Tome III",   Element.Dark,      3, new[]{ ("Shadow Tome II",   2), ("Dark Essence",   8), ("Void Shard",   4), ("Demon Heart",    1) }),
        new EnchantRecipe("Radiant Tome III",  Element.Holy,      3, new[]{ ("Radiant Tome II",  2), ("Holy Dust",      8), ("Sacred Ash",   4), ("Angel Feather",  1) }),
        new EnchantRecipe("Void Codex III",    Element.Void,      3, new[]{ ("Void Codex II",    2), ("Void Fragment",  8), ("Abyssal Core", 4), ("World Shard",    1) }),

        // ── Tier 4 — legendary materials ───────────────────────────────────
        new EnchantRecipe("Blazing Tome IV",   Element.Fire,      4, new[]{ ("Blazing Tome III",  2), ("Ember Shard",   12), ("Magma Core",    6), ("Dragon Scale",   3), ("Primordial Flame",  1) }),
        new EnchantRecipe("Glacial Tome IV",   Element.Ice,       4, new[]{ ("Glacial Tome III",  2), ("Frost Crystal", 12), ("Ice Core",      6), ("Dragon Scale",   3), ("Eternal Frost",     1) }),
        new EnchantRecipe("Storm Tome IV",     Element.Lightning, 4, new[]{ ("Storm Tome III",    2), ("Storm Essence", 12), ("Thunder Core",  6), ("Dragon Scale",   3), ("Storm Heart",       1) }),
        new EnchantRecipe("Earthen Tome IV",   Element.Earth,     4, new[]{ ("Earthen Tome III",  2), ("Stone Fragment",12), ("Earth Core",    6), ("Dragon Scale",   3), ("Ancient Stone",     1) }),
        new EnchantRecipe("Shadow Tome IV",    Element.Dark,      4, new[]{ ("Shadow Tome III",   2), ("Dark Essence",  12), ("Void Shard",    6), ("Demon Heart",    3), ("Soul Crystal",      1) }),
        new EnchantRecipe("Radiant Tome IV",   Element.Holy,      4, new[]{ ("Radiant Tome III",  2), ("Holy Dust",     12), ("Sacred Ash",    6), ("Angel Feather",  3), ("Divine Shard",      1) }),
        new EnchantRecipe("Void Codex IV",     Element.Void,      4, new[]{ ("Void Codex III",    2), ("Void Fragment", 12), ("Abyssal Core",  6), ("World Shard",    3), ("Null Essence",      1) }),

        // ── Tier 5 — endgame, extremely rare ───────────────────────────────
        new EnchantRecipe("Blazing Tome V",    Element.Fire,      5, new[]{ ("Blazing Tome IV",  2), ("Ember Shard",   20), ("Magma Core",   10), ("Primordial Flame", 3), ("World Shard", 1) }),
        new EnchantRecipe("Glacial Tome V",    Element.Ice,       5, new[]{ ("Glacial Tome IV",  2), ("Frost Crystal", 20), ("Ice Core",     10), ("Eternal Frost",    3), ("World Shard", 1) }),
        new EnchantRecipe("Storm Tome V",      Element.Lightning, 5, new[]{ ("Storm Tome IV",    2), ("Storm Essence", 20), ("Thunder Core", 10), ("Storm Heart",      3), ("World Shard", 1) }),
        new EnchantRecipe("Earthen Tome V",    Element.Earth,     5, new[]{ ("Earthen Tome IV",  2), ("Stone Fragment",20), ("Earth Core",   10), ("Ancient Stone",    3), ("World Shard", 1) }),
        new EnchantRecipe("Shadow Tome V",     Element.Dark,      5, new[]{ ("Shadow Tome IV",   2), ("Dark Essence",  20), ("Void Shard",   10), ("Soul Crystal",     3), ("World Shard", 1) }),
        new EnchantRecipe("Radiant Tome V",    Element.Holy,      5, new[]{ ("Radiant Tome IV",  2), ("Holy Dust",     20), ("Sacred Ash",   10), ("Divine Shard",     3), ("World Shard", 1) }),
        new EnchantRecipe("Void Codex V",      Element.Void,      5, new[]{ ("Void Codex IV",    2), ("Void Fragment", 20), ("Abyssal Core", 10), ("Null Essence",     3), ("World Shard", 2) }),
    };

    public static EnchantRecipe? FindByName(string name)
        => Recipes.FirstOrDefault(r => r.BookName.Equals(name, StringComparison.OrdinalIgnoreCase));
}
